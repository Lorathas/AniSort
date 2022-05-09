﻿// Copyright © 2020 Lorathas
//
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation
// files (the “Software”), to deal in the Software without restriction, including without limitation the rights to use, copy,
// modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the
// Software is furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED “AS IS”, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
// OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR
// IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using AniDbSharp;
using AniDbSharp.Data;
using AniSort.Core;
using AniSort.Core.Crypto;
using AniSort.Core.Data;
using AniSort.Core.Data.Repositories;
using AniSort.Core.Exceptions;
using AniSort.Core.Extensions;
using AniSort.Core.IO;
using AniSort.Core.Models;
using AniSort.Core.Utils;
using AniSort.Extensions;
using AniSort.Helpers;
using FFMpegCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NLog;
using NLog.Config;
using NLog.Extensions.Logging;
using NLog.Layouts;
using NLog.Targets;
using LogLevel = NLog.LogLevel;

namespace AniSort
{
    internal class Program
    {
        private const string UsageText =
            @"Usage: anisort.exe [-d | --debug] [-v | --verbose] [-h | --hash] <-u username> <-p password> <paths...>
paths           paths to process files for
-c  --config    config file path
-u  --username  anidb username
-p  --password  anidb password
-h  --hash      hash files and output hashes to console
-d  --debug     enable debug mode to leave files intact and to output suggested actions
-v  --verbose   enable verbose logging";

        private const string ApiClientName = "anidbapiclient";

        private const int ApiClientVersion = 1;

        private static readonly string[] SupportedFileExtensions =
        {
            ".mkv", ".mp4", ".avi", ".wmv", ".mov", ".mpeg", ".mpg", ".flv", ".webm", ".ogg", ".vob", ".m2ts", ".mts",
            ".ts", ".yuv", ".rm", ".rmvb", ".m4v", ".m4p", ".mp2", ".mpe", ".mpv", ".m2v", ".svi", ".3gp", ".3g2",
            ".mxf", ".nsv", ".f4v", ".f4p", ".f4a", ".f4b"
        };

        private static ILogger<Program> logger;

        private static FileImportUtils fileImportUtils;

        private static IServiceProvider serviceProvider;

        private static List<FileImportStatus> importedFiles;

        private static ConsoleProgressBar hashProgressBar;

        private static IAnimeRepository animeRepository;

        private static IEpisodeRepository episodeRepository;

        private static IFileRepository fileRepository;

        private static void Main(string[] args)
        {
            AppPaths.Initialize();

            var config = new Config();

            for (var idx = 0; idx < args.Length; idx++)
            {
                var arg = args[idx];

                if (string.Equals(arg, "-d") || string.Equals(arg, "--debug"))
                {
                    config.Debug = true;
                }
                else if (string.Equals(arg, "-v") || string.Equals(arg, "--verbose"))
                {
                    config.Verbose = true;
                }
                else if (string.Equals(arg, "-h") || string.Equals(arg, "--hash"))
                {
                    config.Mode = Mode.Hash;
                }
                else if (string.Equals(arg, "-u") || string.Equals(arg, "--username"))
                {
                    if (idx == args.Length - 1)
                    {
                        PrintUsageAndExit();
                    }

                    config.AniDb.Username = args[idx + 1];
                    idx++;
                }
                else if (string.Equals(arg, "-p") || string.Equals(arg, "--password"))
                {
                    if (idx == args.Length - 1)
                    {
                        PrintUsageAndExit();
                    }

                    config.AniDb.Password = args[idx + 1];
                    idx++;
                }
                else if (string.Equals(arg, "-c") || string.Equals(arg, "--config"))
                {
                    var configFilePath = args[idx + 1];

                    var serializer = new XmlSerializer(typeof(Config));

                    try
                    {
                        if (!File.Exists(configFilePath))
                        {
                            logger.LogCritical("File does not exist for path: {ConfigFilePath}", configFilePath);
                            Environment.Exit(1);
                        }

                        using var fs = File.OpenRead(configFilePath);
                        config = (Config)serializer.Deserialize(fs);
                    }
                    catch (XmlException ex)
                    {
                        logger.LogCritical(ex, "An error occured when parsing XML of config file at {ConfigFilePath}", configFilePath);
                        Environment.Exit(0);
                    }

                    break;
                }
                else
                {
                    config.Sources.Add(arg);
                }
            }

            if (!config?.IsValid ?? false)
            {
                PrintUsageAndExit();
            }

            AnimeFileStore animeFileStore = null;

            if (File.Exists(AppPaths.AnimeInfoFilePath))
            {
                animeFileStore = new AnimeFileStore();
                animeFileStore.Initialize();
            }

            InitializeLogging(config);
            InitializeDependencyInjection(config);

            importedFiles = fileImportUtils.LoadImportedFiles();

            try
            {
                if (animeFileStore != null || importedFiles != null)
                {
                    var migrateTask = AddExistingDataToDatabaseAsync(animeFileStore, importedFiles);
                    migrateTask.Wait();
                }

                var task = RunAsync(config);
                task.Wait();
            }
            catch (AggregateException ex)
            {
                using var logScope = logger.BeginScope("Aggregate exception occurred while executing sorter functionality");
                ex.Handle(iex =>
                {
                    logger.LogError(ex, "An unhandled error occurred while executing sorter functionality");
                    return false;
                });
            }
        }

        public static async Task AddExistingDataToDatabaseAsync(AnimeFileStore animeFileStore, List<FileImportStatus> importedFiles)
        {

            await using var context = serviceProvider.GetService<AniSortContext>();

            var totalStopwatch = Stopwatch.StartNew();
            
            int createdAnime = 0;
            if (animeFileStore is { Anime.Count: > 0 })
            {
                var existingGroups = context.ReleaseGroups.Select(g => g.Id).Distinct().ToHashSet();

                var existingShows = context.Anime.Select(a => a.Id).Distinct().ToHashSet();

                var storeStopwatch = Stopwatch.StartNew();

                foreach (var anime in animeFileStore.Anime.Values)
                {
                    if (existingShows.Contains(anime.Id))
                    {
                        continue;
                    }
                    var newAnime = new Anime
                    {
                        Id = anime.Id,
                        TotalEpisodes = anime.TotalEpisodes,
                        HighestEpisodeNumber = anime.HighestEpisodeNumber,
                        Year = anime.Year,
                        Type = anime.Type,
                        ChildrenAnime = anime.RelatedAnimeIdList.Select(a => new RelatedAnime { DestinationAnimeId = a.Id, Relation = a.RelationType }).ToList(),
                        RomajiName = anime.RomajiName,
                        KanjiName = anime.KanjiName,
                        EnglishName = anime.EnglishName,
                        OtherName = anime.OtherName,
                        Synonyms = anime.SynonymNames.Select(s => new Synonym { Value = s }).ToList(),
                        Episodes = anime.Episodes.Select(e => new Episode
                        {
                            Id = e.Id,
                            Number = e.Number,
                            EnglishName = e.EnglishName,
                            RomajiName = e.RomajiName,
                            KanjiName = e.KanjiName,
                            Rating = e.Rating,
                            VoteCount = e.VoteCount,
                            Files = e.Files.Select(f => new EpisodeFile
                            {
                                Id = f.Id,
                                GroupId = f.GroupId != 0 ? f.GroupId : ReleaseGroup.UnknownId,
                                OtherEpisodes = f.OtherEpisodes,
                                IsDeprecated = f.IsDeprecated,
                                State = f.State,
                                Ed2kHash = f.Ed2kHash,
                                Md5Hash = f.Md5Hash,
                                Sha1Hash = f.Sha1Hash,
                                Crc32Hash = f.Crc32Hash,
                                VideoColorDepth = f.VideoColorDepth,
                                Quality = f.Quality,
                                Source = f.Source,
                                AudioCodecs = f.AudioCodecs.Select(c => new AudioCodec { Codec = c.CodecName, Bitrate = c.BitrateKbps }).ToList(),
                                VideoCodec = f.VideoCodec.CodecName,
                                VideoBitrate = f.VideoCodec.BitrateKbps,
                                VideoWidth = f.VideoResolution.Width,
                                VideoHeight = f.VideoResolution.Height,
                                FileType = f.FileType,
                                DubLanguage = f.DubLanguage,
                                SubLanguage = f.SubLanguage,
                                LengthInSeconds = f.LengthInSeconds,
                                Description = f.Description,
                                AiredDate = f.AiredDate,
                                AniDbFilename = f.AniDbFilename
                            }).ToList(),
                        }).ToList()
                    };

                    var categoriesAdded = new HashSet<string>();

                    var existingCategories = context.Categories.Where(c => anime.Categories.Contains(c.Value));

                    foreach (var category in existingCategories)
                    {
                        if (categoriesAdded.Contains(category.Value))
                        {
                            continue;
                        }

                        newAnime.Categories.Add(new AnimeCategory { CategoryId = category.Id });

                        categoriesAdded.Add(category.Value);
                    }

                    foreach (var category in anime.Categories)
                    {
                        if (categoriesAdded.Contains(category))
                        {
                            continue;
                        }

                        newAnime.Categories.Add(new AnimeCategory { Category = new Category { Value = category } });

                        categoriesAdded.Add(category);
                    }

                    var groups = anime.Episodes.SelectMany(e => e.Files).Select(f => (f.GroupId, f.GroupName, f.GroupShortName)).Distinct().ToList();

                    foreach (var group in groups)
                    {
                        if (existingGroups.Contains(group.GroupId))
                        {
                            continue;
                        }

                        if (group.GroupId == 0)
                        {
                            if (!existingGroups.Contains(ReleaseGroup.UnknownId))
                            {
                                context.ReleaseGroups.Add(new ReleaseGroup { Id = ReleaseGroup.UnknownId, Name = string.Empty, ShortName = string.Empty });
                            }
                        }
                        else
                        {
                            context.ReleaseGroups.Add(new ReleaseGroup { Id = group.GroupId, Name = group.GroupName, ShortName = group.GroupShortName });
                        }

                        existingGroups.Add(group.GroupId);
                    }

                    context.Anime.Add(newAnime);
                    createdAnime++;
                }

                await context.SaveChangesAsync();

                storeStopwatch.Stop();

                if (animeFileStore.Anime.Count > 0 && createdAnime > 0)
                {
                    logger.LogDebug("Created {CreatedAnime} of {TotalAnime} anime from file store in {ElapsedTime}", createdAnime, animeFileStore.Anime.Count, storeStopwatch.Elapsed);
                }
            }

            int createdFiles = 0;
            if (importedFiles is { Count: > 0 })
            {
                var importsStopwatch = Stopwatch.StartNew();

                var existingFiles = context.LocalFiles.Select(f => f.Path).Distinct().ToHashSet();

                foreach (var fileImportStatus in importedFiles)
                {
                    if (existingFiles.Contains(fileImportStatus.FilePath))
                    {
                        continue;
                    }

                    var localFile = new LocalFile
                    {
                        Path = fileImportStatus.FilePath,
                        Ed2kHash = fileImportStatus.Hash,
                        Status = fileImportStatus.Status,
                        EpisodeFileId = (await context.EpisodeFiles.FirstOrDefaultAsync(f => f.Ed2kHash == fileImportStatus.Hash))?.Id
                    };

                    if (fileImportStatus.Hash != null)
                    {
                        localFile.FileActions.Add(new FileAction
                        {
                            Type = FileActionType.Hash,
                            Success = true,
                            Info = !string.IsNullOrWhiteSpace(fileImportStatus.Message) ? $"Legacy Message: {fileImportStatus.Message}" : null
                        });
                    }

                    for (int idx = fileImportStatus.Status is ImportStatus.Imported or ImportStatus.ImportedMissingData ? 1 : 0; idx < fileImportStatus.Attempts; idx++)
                    {
                        localFile.FileActions.Add(new FileAction
                        {
                            Type = FileActionType.Search,
                            Success = false,
                            Info = !string.IsNullOrWhiteSpace(fileImportStatus.Message) ? $"Legacy Message: {fileImportStatus.Message}" : null
                        });
                    }

                    if (fileImportStatus.Status is ImportStatus.Imported or ImportStatus.ImportedMissingData)
                    {
                        localFile.FileActions.Add(new FileAction
                        {
                            Type = FileActionType.Move,
                            Success = true,
                            Info = !string.IsNullOrWhiteSpace(fileImportStatus.Message) ? $"Legacy Message: {fileImportStatus.Message}" : null
                        });
                    }

                    context.LocalFiles.Add(localFile);
                    createdFiles++;
                }

                await context.SaveChangesAsync();

                importsStopwatch.Stop();
                totalStopwatch.Stop();

                if (importedFiles.Count > 0 && createdFiles > 0)
                {
                    logger.LogDebug("Created {CreatedFile} of {TotalFiles} files from file store in {ElapsedTime}", createdFiles, importedFiles.Count, importsStopwatch.Elapsed);
                }
            }
            if ((importedFiles?.Count > 0 && createdFiles > 0) || (animeFileStore.Anime.Count > 0 && createdAnime > 0))
            {
                logger.LogDebug("Updated database with local files in {ElapsedTime}", totalStopwatch.Elapsed);
            }
        }

        private static void PrintUsageAndExit()
        {
            if (EnvironmentHelpers.IsConsolePresent)
            {
                Console.Write(UsageText);
            }
            else
            {
                // Is this even critical? It is a fail condition for the program...
                logger.LogCritical("Incorrect call of program. Check usage and run again");
            }

            Environment.Exit(0);
        }

        private static async Task RunAsync(Config config)
        {
            switch (config.Mode)
            {
                case Mode.Normal:
                    await RunNormalAsync(config);
                    break;
                case Mode.Hash:
                    RunHashes(config);
                    break;
            }
        }

        private static async Task RunNormalAsync(Config config)
        {
            var fileQueue = new Queue<string>();

            AddPathsToQueue(config.Sources, fileQueue);

            PathBuilder pathBuilder = null;

            try
            {
                pathBuilder = PathBuilder.Compile(
                    config.Destination.NewFilePath,
                    config.Destination.TvPath,
                    config.Destination.MoviePath,
                    config.Destination.Format,
                    new FileMask { FirstByteFlags = FileMaskFirstByte.AnimeId | FileMaskFirstByte.GroupId | FileMaskFirstByte.EpisodeId, SecondByteFlags = FileMaskSecondByte.Ed2k });
            }
            catch (InvalidFormatPathException ex)
            {
                logger.LogCritical(ex, "Invalid path format found in config");
                Environment.Exit(0);
            }

            if (config.Verbose)
            {
                if (EnvironmentHelpers.IsConsolePresent)
                {
                    Console.WriteLine();
                }

                using (logger.BeginScope("Config setup to write to following directories for files:"))
                {
                    logger.LogTrace("TV:     {TvPath}", Path.Combine(config.Destination.NewFilePath, config.Destination.TvPath));
                    logger.LogTrace("Movies: {MoviePath}", Path.Combine(config.Destination.NewFilePath, config.Destination.MoviePath));
                    logger.LogTrace("Path builder base path: {PathBuilderBasePath}", pathBuilder.Root);
                }
            }

            var client = new AniDbClient(ApiClientName, ApiClientVersion, config.AniDb.Username, config.AniDb.Password);

            try
            {
                client.Connect();
                var auth = await client.AuthAsync();

                if (!auth.Success)
                {
                    logger.LogCritical("Invalid auth credentials. Unable to connect to AniDb");
                    Environment.Exit(0);
                }

                if (auth.HasNewVersion)
                {
                    logger.LogWarning("A new version of the software is available. Please download it when possible");
                }

                while (fileQueue.TryDequeue(out var path))
                {
                    try
                    {
                        var filename = Path.GetFileName(path);

                        var fileImportStatus = importedFiles.FirstOrDefault(i => i.FilePath == path);

                        if (fileImportStatus == null)
                        {
                            fileImportStatus = new FileImportStatus(path);
                            importedFiles.Add(fileImportStatus);
                        }

                        if (fileImportStatus.Status == ImportStatus.Imported && await fileRepository.ExistsForHashAsync(fileImportStatus.Hash))
                        {
                            logger.LogDebug("File \"{FilePath}\" has already been imported. Skipping...", path);
                            if (EnvironmentHelpers.IsConsolePresent)
                            {
                                Console.WriteLine();
                            }

                            continue;
                        }

                        byte[] hash;
                        long totalBytes;
                        if (fileImportStatus.Hash != null)
                        {
                            hash = fileImportStatus.Hash;
                            totalBytes = fileImportStatus.FileLength;

                            logger.LogDebug("File \"{FilePath}\" already hashed. Skipping hashing process...", path);
                        }
                        else
                        {
                            await using var fs = new BufferedStream(File.OpenRead(path));
                            fileImportStatus.FileLength = totalBytes = fs.Length;

                            if (EnvironmentHelpers.IsConsolePresent)
                            {
                                hashProgressBar = new ConsoleProgressBar(totalBytes, 40, postfixMessage: $"hashing: {path}",
                                    postfixMessageShort: $"hashing: {filename}");
                            }

                            var sw = Stopwatch.StartNew();

                            var hashTask = Ed2k.HashMultiAsync(fs, new Progress<long>(OnProgressUpdate));

                            while (!hashTask.IsCompleted)
                            {
                                if (EnvironmentHelpers.IsConsolePresent)
                                {
                                    hashProgressBar?.WriteNextFrame();
                                }

                                Thread.Sleep(TimeSpan.FromMilliseconds(100));
                            }

                            hashProgressBar = null;

                            hashTask.Wait();

                            fileImportStatus.Hash = hash = hashTask.Result;

                            sw.Stop();

                            if (EnvironmentHelpers.IsConsolePresent)
                            {
                                Console.Write("\r");
                            }

                            if (EnvironmentHelpers.IsConsolePresent)
                            {
                                logger.LogInformation("Hashed: {TruncatedFilename}", (path.Length + 8 > Console.WindowWidth ? filename : path).Truncate(Console.WindowWidth));
                            }
                            else
                            {
                                logger.LogInformation("Hashed: {Filename}", path);
                            }
                            logger.LogDebug("  eD2k hash: {HashInHex}", hash.ToHexString());

                            if (config.Verbose)
                            {
                                logger.LogTrace(
                                    "  Processed {SizeInMB:###,###,##0.00}MB in {ElapsedTime} at a rate of {HashRate:F2}MB/s", (double)totalBytes / 1024 / 1024, sw.Elapsed,
                                    Math.Round((double)totalBytes / 1024 / 1024 / sw.Elapsed.TotalSeconds));
                            }
                        }

                        var result = await client.SearchForFile(totalBytes, hash, pathBuilder.FileMask, pathBuilder.AnimeMask);

                        if (!result.FileFound)
                        {
                            if (EnvironmentHelpers.IsConsolePresent)
                            {
                                logger.LogWarning($"No file found for {filename}".Truncate(Console.WindowWidth));
                            }
                            else
                            {
                                logger.LogWarning("No file found for {FilePath}", filename);
                            }

                            if (EnvironmentHelpers.IsConsolePresent)
                            {
                                Console.WriteLine();
                            }

                            fileImportStatus.Status = ImportStatus.NoFileFound;
                            await fileImportUtils.UpdateImportedFilesAsync(importedFiles);
                            continue;
                        }

                        logger.LogInformation($"File found for {filename}");

                        if (config.Verbose)
                        {
                            logger.LogTrace("  Anime: {AnimeNameInRomaji}", result.AnimeInfo.RomajiName);
                            logger.LogTrace("  Episode: {EpisodeNumber:##} {EpisodeName}", result.AnimeInfo.EpisodeNumber, result.AnimeInfo.EpisodeName);
                            logger.LogTrace("  CRC32: {Crc32Hash}", result.FileInfo.Crc32Hash.ToHexString());
                            logger.LogTrace("  Group: {SubGroupName}", result.AnimeInfo.GroupShortName);
                        }

                        var anime = result.ToAnimeInfo();

                        animeRepository.MergeSert(anime);
                        await animeRepository.SaveChangesAsync();

                        var resolution = result.FileInfo.VideoResolution.ParseVideoResolution();

                        if (!result.FileInfo.HasResolution)
                        {
                            var mediaInfo = await FFProbe.AnalyseAsync(path);

                            resolution = new VideoResolution(mediaInfo.PrimaryVideoStream.Width, mediaInfo.PrimaryVideoStream.Height);
                        }

                        var extension = Path.GetExtension(filename);

                        // Trailing dot is there to prevent Path.ChangeExtension from screwing with the path if it has been ellipsized or has ellipsis in it
                        var destinationPath = pathBuilder.BuildPath(result.FileInfo, result.AnimeInfo,
                            PlatformUtils.MaxPathLength - extension.Length, resolution);

                        var destinationFilename = destinationPath + extension;
                        var destinationDirectory = Path.GetDirectoryName(destinationPath);

                        if (!config.Debug && !Directory.Exists(destinationDirectory))
                        {
                            try
                            {
                                Directory.CreateDirectory(destinationDirectory);
                            }
                            catch (Exception ex)
                            {
                                logger.LogError(ex,
                                    "An unknown error occurred while trying to created the directory. Please make sure the program has access to the target directory.");
                                if (EnvironmentHelpers.IsConsolePresent)
                                {
                                    Console.WriteLine();
                                }

                                fileImportStatus.Status = ImportStatus.Error;
                                fileImportStatus.Message = ex.Message;
                                fileImportStatus.Attempts++;
                                fileImportUtils.UpdateImportedFiles(importedFiles);
                                continue;
                            }
                        }

                        if (File.Exists(destinationFilename))
                        {
                            fileImportStatus.Status = result.FileInfo.HasResolution ? ImportStatus.Imported : ImportStatus.ImportedMissingData;
                            fileImportStatus.Message = destinationFilename;
                            fileImportStatus.Attempts++;
                            fileImportUtils.UpdateImportedFiles(importedFiles);
                            logger.LogDebug("Destination file \"{DestinationFilename}\" already exists. Skipping...", destinationFilename);
                        }
                        else if (config.Copy)
                        {
                            if (!config.Debug)
                            {
                                try
                                {
                                    if (config.Verbose)
                                    {
                                        logger.LogTrace("Destination Path: {DestinationFilename}", destinationFilename);
                                    }

                                    File.Copy(path, destinationFilename);
                                }
                                catch (UnauthorizedAccessException ex)
                                {
                                    logger.LogError("You do not have access to the destination path. Please ensure your user account has access to the destination folder.");
                                    if (EnvironmentHelpers.IsConsolePresent)
                                    {
                                        Console.WriteLine();
                                    }

                                    fileImportStatus.Status = ImportStatus.Error;
                                    fileImportStatus.Message = ex.Message;
                                    fileImportStatus.Attempts++;
                                    fileImportUtils.UpdateImportedFiles(importedFiles);
                                    continue;
                                }
                                catch (PathTooLongException ex)
                                {
                                    logger.LogError(
                                        "Filename too long. Yell at Lorathas to fix path length checking if this keeps occurring.");
                                    if (EnvironmentHelpers.IsConsolePresent)
                                    {
                                        Console.WriteLine();
                                    }

                                    fileImportStatus.Status = ImportStatus.Error;
                                    fileImportStatus.Message = ex.Message;
                                    fileImportStatus.Attempts++;
                                    fileImportUtils.UpdateImportedFiles(importedFiles);
                                    continue;
                                }
                                catch (IOException ex)
                                {
                                    logger.LogError(ex, "An unhandled I/O error has occurred");
                                    if (EnvironmentHelpers.IsConsolePresent)
                                    {
                                        Console.WriteLine();
                                    }

                                    fileImportStatus.Status = ImportStatus.Error;
                                    fileImportStatus.Message = ex.Message;
                                    fileImportStatus.Attempts++;
                                    fileImportUtils.UpdateImportedFiles(importedFiles);
                                    continue;
                                }
                            }

                            fileImportStatus.Status = result.FileInfo.HasResolution ? ImportStatus.Imported : ImportStatus.ImportedMissingData;
                            fileImportStatus.Message = destinationFilename;
                            fileImportStatus.Attempts++;
                            fileImportUtils.UpdateImportedFiles(importedFiles);
                            logger.LogInformation("Copied {SourceFilePath} to {DestinationFilePath}", filename, destinationFilename);
                        }
                        else
                        {
                            if (!config.Debug)
                            {
                                try
                                {
                                    File.Move(path, destinationFilename);
                                }
                                catch (UnauthorizedAccessException ex)
                                {
                                    logger.LogError(ex,
                                        "You do not have access to the destination path. Please ensure your user account has access to the destination folder.");
                                    if (EnvironmentHelpers.IsConsolePresent)
                                    {
                                        Console.WriteLine();
                                    }

                                    fileImportStatus.Status = ImportStatus.Error;
                                    fileImportStatus.Message = ex.Message;
                                    fileImportStatus.Attempts++;
                                    fileImportUtils.UpdateImportedFiles(importedFiles);
                                    continue;
                                }
                                catch (PathTooLongException ex)
                                {
                                    logger.LogError(ex,
                                        "Filename too long. Yell at Lorathas to implement path length checking if this keeps occurring.");
                                    if (EnvironmentHelpers.IsConsolePresent)
                                    {
                                        Console.WriteLine();
                                    }

                                    fileImportStatus.Status = ImportStatus.Error;
                                    fileImportStatus.Message = ex.Message;
                                    fileImportStatus.Attempts++;
                                    fileImportUtils.UpdateImportedFiles(importedFiles);
                                    continue;
                                }
                                catch (IOException ex)
                                {
                                    logger.LogError(ex, "An unhandled I/O error has occurred");
                                    if (EnvironmentHelpers.IsConsolePresent)
                                    {
                                        Console.WriteLine();
                                    }

                                    fileImportStatus.Status = ImportStatus.Error;
                                    fileImportStatus.Message = ex.Message;
                                    fileImportStatus.Attempts++;
                                    fileImportUtils.UpdateImportedFiles(importedFiles);
                                    continue;
                                }
                            }

                            fileImportStatus.Status = ImportStatus.Imported;
                            fileImportStatus.Message = destinationFilename;
                            fileImportStatus.Attempts++;
                            fileImportUtils.UpdateImportedFiles(importedFiles);
                            logger.LogInformation("Moved {SourceFilePath} to {DestinationFilePath}", filename, destinationFilename);
                        }

                        if (EnvironmentHelpers.IsConsolePresent)
                        {
                            Console.WriteLine();
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, ex.Message);
                    }
                }
            }
            finally
            {
                client.Dispose();
            }

            logger.LogInformation("Finished processing all files. Exiting...");
        }

        private static void AddPathsToQueue(IEnumerable<string> paths, Queue<string> queue)
        {
            foreach (var path in paths)
            {
                if (Directory.Exists(path))
                {
                    AddPathsToQueue(Directory.GetFiles(path).Concat(Directory.GetDirectories(path)), queue);
                }
                else if (SupportedFileExtensions.Contains(Path.GetExtension(path)) && File.Exists(path))
                {
                    queue.Enqueue(path);
                }
            }
        }

        private static void OnProgressUpdate(long bytesProcessed)
        {
            if (hashProgressBar != null)
            {
                hashProgressBar.Progress = bytesProcessed;
            }
        }

        private static void RunHashes(Config config)
        {
            var fileQueue = new Queue<string>();

            AddPathsToQueue(config.Sources, fileQueue);

            while (fileQueue.TryDequeue(out var path))
            {
                using (var fs = new BufferedStream(File.OpenRead(path)))
                {
                    var totalBytes = fs.Length;

                    hashProgressBar = new ConsoleProgressBar(totalBytes, 40, postfixMessage: $"hashing: {path}",
                        postfixMessageShort: $"hashing: {Path.GetFileName(path)}");

                    var sw = Stopwatch.StartNew();

                    var hashTask = Ed2k.HashMultiAsync(fs, new Progress<long>(OnProgressUpdate));

                    while (!hashTask.IsCompleted)
                    {
                        if (EnvironmentHelpers.IsConsolePresent)
                        {
                            hashProgressBar?.WriteNextFrame();
                        }

                        Thread.Sleep(TimeSpan.FromMilliseconds(100));
                    }

                    hashTask.Wait();

                    var hash = hashTask.Result;

                    hashProgressBar = null;

                    sw.Stop();

                    if (EnvironmentHelpers.IsConsolePresent)
                    {
                        logger.LogInformation(
                            $"\rHashed: {(path.Length + 8 > Console.WindowWidth ? Path.GetFileName(path) : path).Truncate(Console.WindowWidth)}");
                    }
                    else
                    {
                        logger.LogInformation("Hashed: {FilePath}", path);
                    }

                    logger.LogTrace($"  eD2k hash: {hash.ToHexString()}");

                    logger.LogTrace(
                        $"  Processed {(double)totalBytes / 1024 / 1024:###,###,##0.00}MB in {sw.Elapsed} at a rate of {Math.Round((double)totalBytes / 1024 / 1024 / sw.Elapsed.TotalSeconds):F2}MB/s");

                    if (EnvironmentHelpers.IsConsolePresent)
                    {
                        Console.WriteLine();
                    }
                }
            }

            logger.LogInformation("Finished hashing all files. Exiting...");
        }

        private static void InitializeLogging(Config aniSortConfig)
        {
            var config = new LoggingConfiguration();

            var fileLog = new FileTarget("fileLog")
            {
                FileName = Path.Combine(AppPaths.DataPath, "anisort.log")
            };
            var errorFileLog = new FileTarget("errorFileLog")
            {
                FileName = Path.Combine(AppPaths.DataPath, "anisort.err.log"),
                Layout = new SimpleLayout("${longdate}|${level:uppercase=true}|${logger}|${message}|${exception:format=StackTrace}")
            };

            var fileAndConsoleMinLevel = LogLevel.Info;

            if (aniSortConfig.Verbose)
            {
                fileAndConsoleMinLevel = LogLevel.Debug;
            }
            else if (aniSortConfig.Debug)
            {
                fileAndConsoleMinLevel = LogLevel.Debug;
            }

            config.AddRule(fileAndConsoleMinLevel, LogLevel.Warn, fileLog);
            config.AddRule(LogLevel.Error, LogLevel.Fatal, errorFileLog);

            if (EnvironmentHelpers.IsConsolePresent)
            {
                var consoleLog = new ConsoleTarget("consoleLog");
                config.AddRule(fileAndConsoleMinLevel, LogLevel.Fatal, consoleLog);
            }

            LogManager.Configuration = config;
        }

        private static void InitializeDependencyInjection(Config config)
        {
            serviceProvider = new ServiceCollection()
                .AddSingleton(config)
                .AddSingleton(typeof(FileImportUtils))
                .AddTransient<IAnimeRepository, LocalAnimeRepository>()
                .AddTransient<IEpisodeRepository, LocalEpisodeRepository>()
                .AddTransient<IFileRepository, LocalFileRepository>()
                .AddDbContext<AniSortContext>(builder => builder.UseSqlite($"Data Source={AppPaths.DatabasePath}"))
                .AddLogging(b =>
                {
                    b.AddFilter((category, logLevel) =>
                    {
                        if (category.StartsWith("Microsoft.EntityFrameworkCore")
                            && logLevel != Microsoft.Extensions.Logging.LogLevel.Critical
                            && logLevel != Microsoft.Extensions.Logging.LogLevel.Error
                            && logLevel != Microsoft.Extensions.Logging.LogLevel.Warning)
                        {
                            return false;
                        }

                        return true;
                    });
                    b.ClearProviders();
                    b.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Trace);
                    b.AddNLog();
                })
                .BuildServiceProvider();

            logger = serviceProvider.GetService<ILogger<Program>>();
            fileImportUtils = serviceProvider.GetService<FileImportUtils>();
            animeRepository = serviceProvider.GetService<IAnimeRepository>();
            episodeRepository = serviceProvider.GetService<IEpisodeRepository>();
            fileRepository = serviceProvider.GetService<IFileRepository>();
        }
    }
}
