using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AniDbSharp;
using AniDbSharp.Data;
using AniDbSharp.Exceptions;
using AniSort.Core.Crypto;
using AniSort.Core.Data;
using AniSort.Core.Data.Repositories;
using AniSort.Core.Exceptions;
using AniSort.Core.Extensions;
using AniSort.Core.Helpers;
using AniSort.Core.IO;
using AniSort.Core.Models;
using AniSort.Core.Utils;
using FFMpegCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.CommandLineUtils;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AniSort.Core.Commands;

public class SortCommand : ICommand
{
    private readonly Config config;
    private readonly ILogger<SortCommand> logger;
    private readonly AniDbClient client;
    private readonly IServiceProvider serviceProvider;
    private readonly IPathBuilderRepository pathBuilderRepository;
    private ConsoleProgressBar hashProgressBar;

    public SortCommand(Config config, ILogger<SortCommand> logger, AniDbClient client, IServiceProvider serviceProvider, IPathBuilderRepository pathBuilderRepository)
    {
        this.config = config;
        this.logger = logger;
        this.client = client;
        this.serviceProvider = serviceProvider;
        this.pathBuilderRepository = pathBuilderRepository;
    }

    /// <inheritdoc />
    public async Task RunAsync(List<CommandOption> commandOptions)
    {var fileQueue = new Queue<string>();
     
             fileQueue.AddPathsToQueue(config.Sources);
             if (!config.IgnoreLibraryFiles)
             {
                 fileQueue.AddPathsToQueue(config.LibraryPaths);
             }
     
             if (config.Verbose)
             {
                 if (EnvironmentHelpers.IsConsolePresent)
                 {
                     Console.WriteLine();
                 }
     
                 using (logger.BeginScope("Config setup to write to following directories for files:"))
                 {
                     logger.LogTrace("TV:     {TvPath}", Path.Combine(config.Destination.Path, config.Destination.TvPath));
                     logger.LogTrace("Movies: {MoviePath}", Path.Combine(config.Destination.Path, config.Destination.MoviePath));
                     logger.LogTrace("Path builder base path: {PathBuilderBasePath}", pathBuilderRepository.DefaultPathBuilder.Root);
                 }
             }
     
             try
             {
                 client.Connect();
                 var auth = await client.AuthAsync();
     
                 if (!auth.Success)
                 {
                     logger.LogCritical("Invalid auth credentials. Unable to connect to AniDb");
                     Environment.Exit(ExitCodes.InvalidAuthCredentials);
                 }
     
                 if (auth.HasNewVersion)
                 {
                     logger.LogWarning("A new version of the software is available. Please download it when possible");
                 }
     
                 await using var scope = serviceProvider.CreateAsyncScope();
     
                 var localFileRepository = scope.ServiceProvider.GetService<ILocalFileRepository>();
                 var animeRepository = scope.ServiceProvider.GetService<IAnimeRepository>();
                 var episodeRepository = scope.ServiceProvider.GetService<IEpisodeRepository>();
                 var episodeFileRepository = scope.ServiceProvider.GetService<IEpisodeFileRepository>();
                 var actionRepository = scope.ServiceProvider.GetService<IFileActionRepository>();
     
                 while (fileQueue.TryDequeue(out var path))
                 {
                     try
                     {
                         var filename = Path.GetFileName(path);

                         var localFile = await localFileRepository.GetForPathAsync(path);

                         if (localFile == null)
                         {
                             localFile = new LocalFile { Path = path, Status = ImportStatus.NotYetImported, EpisodeFile = null };
                             await localFileRepository.AddAsync(localFile);
                             await localFileRepository.SaveChangesAsync();
                         }

                         if (localFile.Status == ImportStatus.Imported)
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
                         if (localFile.Ed2kHash != null)
                         {
                             hash = localFile.Ed2kHash;
                             totalBytes = localFile.FileLength;

                             logger.LogDebug("File \"{FilePath}\" already hashed. Skipping hashing process...", path);
                         }
                         else
                         {
                             var hashAction = new FileAction { Type = FileActionType.Hash, Success = false, FileId = localFile.Id };
                             await actionRepository.AddAsync(hashAction);
                             await actionRepository.SaveChangesAsync();

                             await using var fs = new BufferedStream(File.OpenRead(path));
                             localFile.FileLength = totalBytes = fs.Length;
                             localFile.UpdatedAt = DateTimeOffset.Now;
                             await localFileRepository.SaveChangesAsync();

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

                             localFile.Ed2kHash = hash = hashTask.Result;
                             localFile.Status = ImportStatus.Hashed;
                             localFile.UpdatedAt = DateTimeOffset.Now;
                             if (hashAction != null)
                             {
                                 hashAction.Success = true;
                                 hashAction.Info = $"Successfully hashed file with hash of {hashTask.Result.ToHexString()}";
                                 hashAction.UpdatedAt = DateTimeOffset.Now;
                             }
                             await localFileRepository.SaveChangesAsync();

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

                         var fileActions = actionRepository.GetForFile(localFile.Id).ToList().OrderBy(a => a.CreatedAt).ToList();

                         if (config.AniDb.MaxFileSearchRetries.HasValue && fileActions.Count(a => a.Type == FileActionType.Search) >= config.AniDb.MaxFileSearchRetries)
                         {
                             if (EnvironmentHelpers.IsConsolePresent)
                             {
                                 logger.LogDebug("File {TruncatedFilename} has hit the retry limit, skipping", (path.Length + 40 > Console.WindowWidth ? filename : path).Truncate(Console.WindowWidth));
                             }
                             else
                             {
                                 logger.LogDebug("File {Filename} has hit the retry limit, skipping", path);
                             }
                             continue;
                         }
                         
                         var lastSearchAction = fileActions.LastOrDefault(a => a.Type == FileActionType.Search);

                         if (config.AniDb.FileSearchCooldown != TimeSpan.Zero && (lastSearchAction?.IsCoolingDown(config.AniDb.FileSearchCooldown) ?? false))
                         {
                             if (EnvironmentHelpers.IsConsolePresent)
                             {
                                 logger.LogDebug("File {TruncatedFilename} is still cooling down from last search, skipping",
                                     (path.Length + 49 + 5 > Console.WindowWidth ? filename : path).Truncate(Console.WindowWidth));
                             }
                             else
                             {
                                 logger.LogDebug("File {Filename} is still cooling down from last search, skipping", path);
                             }
                             continue;
                         }

                         var searchAction = new FileAction { Type = FileActionType.Search, Success = false, FileId = localFile.Id };
                         await actionRepository.AddAsync(searchAction);
                         await actionRepository.SaveChangesAsync();
                         
                         fileActions.Add(searchAction);
                         fileActions = fileActions.OrderBy(a => a.CreatedAt).ToList();

                         var pathBuilder = pathBuilderRepository.GetPathBuilderForPath(path);

                         var result = await client.SearchForFile(totalBytes, hash, pathBuilder.FileMask, pathBuilder.AnimeMask);

                         if (!result.FileFound)
                         {
                             searchAction.Info = "No file found for hash";
                             searchAction.UpdatedAt = DateTimeOffset.Now;
                             await localFileRepository.SaveChangesAsync();

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

                             localFile.Status = ImportStatus.NoFileFound;
                             localFile.UpdatedAt = DateTimeOffset.Now;
                             continue;
                         }

                         searchAction.Success = true;
                         searchAction.Info = $"Found file {result.FileInfo.FileId} for file hash {localFile.Ed2kHash.ToHexString()}";
                         searchAction.UpdatedAt = DateTimeOffset.Now;
                         await actionRepository.SaveChangesAsync();
                         await localFileRepository.SaveChangesAsync();

                         logger.LogInformation($"File found for {filename}");

                         if (config.Verbose)
                         {
                             logger.LogTrace("  Anime: {AnimeNameInRomaji}", result.AnimeInfo.RomajiName);
                             logger.LogTrace("  Episode: {EpisodeNumber:##} {EpisodeName}", result.AnimeInfo.EpisodeNumber, result.AnimeInfo.EpisodeName);
                             logger.LogTrace("  CRC32: {Crc32Hash}", result.FileInfo.Crc32Hash.ToHexString());
                             logger.LogTrace("  Group: {SubGroupName}", result.AnimeInfo.GroupShortName);
                         }

                         var (anime, episode, episodeFile) = await animeRepository.MergeSertAsync(result, false);
                         await animeRepository.SaveChangesAsync();
                         if (episode.Id == 0)
                         {
                             episode.AnimeId = anime.Id;
                             await episodeRepository.AddAsync(episode);
                             await episodeRepository.SaveChangesAsync();
                         }
                         if (episodeFile.Id == 0)
                         {
                             episodeFile.EpisodeId = episode.Id;
                             await episodeFileRepository.AddAsync(episodeFile);
                             await episodeFileRepository.SaveChangesAsync();
                         }
                         localFile.EpisodeFileId = episodeFile.Id;
                         await localFileRepository.SaveChangesAsync();


                         var resolution = result.FileInfo.VideoResolution.ParseVideoResolution();

                         if (!result.FileInfo.HasResolution)
                         {
                             var mediaInfo = await FFProbe.AnalyseAsync(path);

                             resolution = new VideoResolution(mediaInfo.PrimaryVideoStream.Width, mediaInfo.PrimaryVideoStream.Height);
                         }

                         var extension = Path.GetExtension(filename);

                         // Trailing dot is there to prevent Path.ChangeExtension from screwing with the path if it has been ellipsized or has ellipsis in it
                         var destinationPathWithoutExtension = pathBuilder.BuildPath(result.FileInfo, result.AnimeInfo,
                             PlatformUtils.MaxPathLength - extension.Length, resolution);

                         var destinationPath = destinationPathWithoutExtension + extension;
                         var destinationDirectory = Path.GetDirectoryName(destinationPathWithoutExtension);

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

                                 localFile.Status = ImportStatus.Error;
                                 localFile.UpdatedAt = DateTimeOffset.Now;
                                 await localFileRepository.SaveChangesAsync();
                                 continue;
                             }
                         }

                         if (File.Exists(destinationPath))
                         {
                             localFile.Status = result.FileInfo.HasResolution ? ImportStatus.Imported : ImportStatus.ImportedMissingData;
                             localFile.UpdatedAt = DateTimeOffset.Now;
                             await actionRepository.AddAsync(new FileAction { Type = FileActionType.Copied, Success = true, Info = $"File already exists at {destinationPath}", FileId = localFile.Id });
                             await actionRepository.SaveChangesAsync();
                             if (!await localFileRepository.ExistsForPathAsync(localFile.Path))
                             {
                                 await localFileRepository.AddAsync(new LocalFile
                                 {
                                     Path = localFile.Path,
                                     Status = localFile.Status,
                                     Ed2kHash = localFile.Ed2kHash,
                                     EpisodeFileId = localFile.EpisodeFileId,
                                     FileLength = localFile.FileLength,
                                     FileActions = new List<FileAction> { new() { Type = FileActionType.Copied, Success = true, Info = $"File already exists at {destinationPath}" } }
                                 });
                             }
                             await localFileRepository.SaveChangesAsync();
                             logger.LogDebug("Destination file \"{DestinationPath}\" already exists. Skipping...", destinationPath);
                         }
                         else if (config.Copy)
                         {
                             if (!config.Debug)
                             {
                                 try
                                 {
                                     if (config.Verbose)
                                     {
                                         logger.LogTrace("Destination Path: {DestinationPath}", destinationPath);
                                     }

                                     File.Copy(path, destinationPath);
                                 }
                                 catch (UnauthorizedAccessException ex)
                                 {
                                     logger.LogError("You do not have access to the destination path. Please ensure your user account has access to the destination folder.");
                                     if (EnvironmentHelpers.IsConsolePresent)
                                     {
                                         Console.WriteLine();
                                     }

                                     localFile.Status = ImportStatus.Error;
                                     localFile.UpdatedAt = DateTimeOffset.Now;
                                     await localFileRepository.SaveChangesAsync();
                                     await actionRepository.AddAsync(new FileAction
                                         { Type = config.Copy ? FileActionType.Copy : FileActionType.Move, Success = false, Exception = $"{ex.Message}\n{ex.StackTrace}", FileId = localFile.Id });
                                     await actionRepository.SaveChangesAsync();
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

                                     localFile.Status = ImportStatus.Error;
                                     localFile.UpdatedAt = DateTimeOffset.Now;
                                     await localFileRepository.SaveChangesAsync();
                                     await actionRepository.AddAsync(new FileAction
                                         { Type = config.Copy ? FileActionType.Copy : FileActionType.Move, Success = false, Exception = $"{ex.Message}\n{ex.StackTrace}", FileId = localFile.Id });
                                     await actionRepository.SaveChangesAsync();
                                     continue;
                                 }
                                 catch (IOException ex)
                                 {
                                     logger.LogError(ex, "An unhandled I/O error has occurred");
                                     if (EnvironmentHelpers.IsConsolePresent)
                                     {
                                         Console.WriteLine();
                                     }

                                     localFile.Status = ImportStatus.Error;
                                     localFile.UpdatedAt = DateTimeOffset.Now;
                                     await localFileRepository.SaveChangesAsync();
                                     await actionRepository.AddAsync(new FileAction
                                         { Type = config.Copy ? FileActionType.Copy : FileActionType.Move, Success = false, Exception = $"{ex.Message}\n{ex.StackTrace}", FileId = localFile.Id });
                                     await actionRepository.SaveChangesAsync();
                                     continue;
                                 }

                                 localFile.Status = result.FileInfo.HasResolution ? ImportStatus.Imported : ImportStatus.ImportedMissingData;
                                 await localFileRepository.AddAsync(new LocalFile
                                 {
                                     Path = localFile.Path,
                                     Status = localFile.Status,
                                     Ed2kHash = localFile.Ed2kHash,
                                     EpisodeFileId = localFile.EpisodeFileId,
                                     FileLength = localFile.FileLength,
                                     FileActions = new List<FileAction> { new() { Type = FileActionType.Copied, Success = true, Info = $"Source file copied to {destinationPath}" } }
                                 });
                                 localFile.Path = destinationPath;
                                 localFile.UpdatedAt = DateTimeOffset.Now;
                                 await localFileRepository.SaveChangesAsync();
                                 await actionRepository.AddAsync(new FileAction
                                 {
                                     Type = FileActionType.Copy,
                                     Success = true,
                                     Info = $"File {localFile.Path} copied to {destinationPath}",
                                     FileId = localFile.Id
                                 });
                                 await actionRepository.SaveChangesAsync();
                             }

                             logger.LogInformation("Copied {SourceFilePath} to {DestinationFilePath}", filename, destinationPath);
                         }
                         else
                         {
                             if (!config.Debug)
                             {
                                 try
                                 {
                                     File.Move(path, destinationPath);
                                 }
                                 catch (UnauthorizedAccessException ex)
                                 {
                                     logger.LogError(ex,
                                         "You do not have access to the destination path. Please ensure your user account has access to the destination folder.");
                                     if (EnvironmentHelpers.IsConsolePresent)
                                     {
                                         Console.WriteLine();
                                     }

                                     localFile.Status = ImportStatus.Error;
                                     localFile.UpdatedAt = DateTimeOffset.Now;
                                     await localFileRepository.SaveChangesAsync();
                                     await actionRepository.AddAsync(new FileAction
                                         { Type = config.Copy ? FileActionType.Copy : FileActionType.Move, Success = false, Exception = $"{ex.Message}\n{ex.StackTrace}", FileId = localFile.Id });
                                     await actionRepository.SaveChangesAsync();
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

                                     localFile.Status = ImportStatus.Error;
                                     localFile.UpdatedAt = DateTimeOffset.Now;
                                     await localFileRepository.SaveChangesAsync();
                                     await actionRepository.AddAsync(new FileAction
                                         { Type = config.Copy ? FileActionType.Copy : FileActionType.Move, Success = false, Exception = $"{ex.Message}\n{ex.StackTrace}", FileId = localFile.Id });
                                     await actionRepository.SaveChangesAsync();
                                     continue;
                                 }
                                 catch (IOException ex)
                                 {
                                     logger.LogError(ex, "An unhandled I/O error has occurred");
                                     if (EnvironmentHelpers.IsConsolePresent)
                                     {
                                         Console.WriteLine();
                                     }

                                     localFile.Status = ImportStatus.Error;
                                     localFile.UpdatedAt = DateTimeOffset.Now;
                                     await localFileRepository.SaveChangesAsync();
                                     await actionRepository.AddAsync(new FileAction
                                         { Type = config.Copy ? FileActionType.Copy : FileActionType.Move, Success = false, Exception = $"{ex.Message}\n{ex.StackTrace}", FileId = localFile.Id });
                                     await actionRepository.SaveChangesAsync();
                                     continue;
                                 }

                                 localFile.Status = ImportStatus.Imported;
                                 localFile.UpdatedAt = DateTimeOffset.Now;
                                 localFile.Path = destinationPath;
                                 await localFileRepository.SaveChangesAsync();
                                 await actionRepository.AddAsync(new FileAction
                                 {
                                     Type = FileActionType.Move,
                                     Success = true,
                                     Info = $"File {localFile.Path} moved to {destinationPath}",
                                     FileId = localFile.Id
                                 });
                                 await actionRepository.SaveChangesAsync();
                             }

                             logger.LogInformation("Moved {SourceFilePath} to {DestinationFilePath}", filename, destinationPath);
                         }

                         if (EnvironmentHelpers.IsConsolePresent)
                         {
                             Console.WriteLine();
                         }
                     }
                     catch (AniDbConnectionRefusedException ex)
                     {
                         logger.LogCritical(ex, "AniDB connection timed out. Please wait or switch to a different IP address.");
                         Environment.Exit(ExitCodes.AniDbConnectionRefused);
                     }
                     catch (DbUpdateConcurrencyException ex)
                     {
                         foreach (var entry in ex.Entries)
                         {
                             logger.LogError(ex, "An issue occurred while trying to update the entity {Entity}", entry);
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
                 await client.DisposeAsync();
             }
    }
    
    private void OnProgressUpdate(long bytesProcessed)
    {
        if (hashProgressBar != null)
        {
            hashProgressBar.Progress = bytesProcessed;
        }
    }

    /// <inheritdoc />
    public IEnumerable<string> CommandNames => new[] { "sort" };

    /// <inheritdoc />
    public string HelpOption => "-h --help";

    /// <inheritdoc />
    public bool IncludeCredentialOptions => true;

    /// <inheritdoc />
    public List<CommandOption> SetupCommand(CommandLineApplication command) => new();
}
