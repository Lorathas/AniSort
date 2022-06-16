// // Copyright © 2022 Lorathas
// //
// // Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation
// // files (the “Software”), to deal in the Software without restriction, including without limitation the rights to use, copy,
// // modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the
// // Software is furnished to do so, subject to the following conditions:
// //
// // The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
// //
// // THE SOFTWARE IS PROVIDED “AS IS”, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
// // OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// // LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR
// // IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using AniDbSharp;
using AniDbSharp.Data;
using AniDbSharp.Exceptions;
using AniSort.Core.Crypto;
using AniSort.Core.Data;
using AniSort.Core.Data.Repositories;
using AniSort.Core.Extensions;
using AniSort.Core.Helpers;
using AniSort.Core.IO;
using AniSort.Core.Models;
using AniSort.Core.Utils;
using FFMpegCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using FileInfo = AniDbSharp.Data.FileInfo;

namespace AniSort.Core.DataFlow;

public class BlockProvider
{
    private readonly Config config;
    private readonly IServiceProvider serviceProvider;

    public BlockProvider(Config config, IServiceProvider serviceProvider)
    {
        this.config = config;
        this.serviceProvider = serviceProvider;
    }

    /// <summary>
    /// Build a transform block to fetch possible existing local file data, or create new data for a path
    /// </summary>
    /// <param name="options">Optional dataflow execution options</param>
    /// <returns></returns>
    public TransformBlock<string, LocalFile> BuildFetchLocalFileBlock(ExecutionDataflowBlockOptions options = null)
    {
        options ??= new ExecutionDataflowBlockOptions();

        int calls = 0;

        return new TransformBlock<string, LocalFile>(async path =>
        {
            var logger = serviceProvider.GetService<ILogger<BlockProvider>>() ?? throw new ApplicationException($"Unable to instantiate type {typeof(ILogger)}");
            var localFileRepository = serviceProvider.GetService<ILocalFileRepository>() ?? throw new ApplicationException($"Unable to instantiate type {typeof(ILocalFileRepository)}");

            using var logScope = logger.BeginScope("FetchLocalFileBlock");

            try
            {
                LocalFile localFile;
                await AniSortContext.DatabaseLock.WaitAsync();
                try
                {
                    localFile = await localFileRepository!.GetForPathAsync(path);
                }
                finally
                {
                    AniSortContext.DatabaseLock.Release();
                }

                if (localFile == null)
                {
                    localFile = new LocalFile { Path = path, Status = ImportStatus.NotYetImported, EpisodeFile = null };
                    if (!config.Debug)
                    {
                        await AniSortContext.DatabaseLock.WaitAsync();
                        try
                        {
                            await localFileRepository.AddAsync(localFile);
                            await localFileRepository.SaveChangesAsync();
                        }
                        finally
                        {
                            AniSortContext.DatabaseLock.Release();
                        }
                    }
                }

                if (localFile.Status == ImportStatus.Imported)
                {
                    logger!.LogDebug("File \"{FilePath}\" has already been imported. Skipping...", path);

                    return null;
                }

                return localFile;
            }
            catch (AniDbConnectionRefusedException ex)
            {
                logger.LogCritical(ex, "AniDB connection timed out. Please wait or switch to a different IP address");
                Environment.Exit(ExitCodes.AniDbConnectionRefused);
                return default;
            }
            catch (DbUpdateConcurrencyException ex)
            {
                foreach (var entry in ex.Entries)
                {
                    logger.LogError(ex, "An issue occurred while trying to update the entity {Entity}", entry);
                }
                return default;
            }
            catch (Exception ex)
            {
                // ReSharper disable once TemplateIsNotCompileTimeConstantProblem
                logger.LogError(ex, ex.Message);
                return default;
            }
        }, options);
    }

    /// <summary>
    /// Build a transform block to hash a file
    /// </summary>
    /// <param name="onProgressUpdate">Progress update function to call when hashing</param>
    /// <param name="options">Optional dataflow execution options</param>
    /// <returns></returns>
    public TransformBlock<LocalFile, LocalFile> BuildHashFileBlock(Action<string, long> onNewHashStarted, Action<long> onProgressUpdate, Action onHashFinished, ExecutionDataflowBlockOptions options = null)
    {
        options ??= new ExecutionDataflowBlockOptions();

        return new TransformBlock<LocalFile, LocalFile>(async localFile =>
        {
            var logger = serviceProvider.GetService<ILogger<BlockProvider>>() ?? throw new ApplicationException($"Unable to instantiate type {typeof(ILogger)}");
            var localFileRepository = serviceProvider.GetService<ILocalFileRepository>() ?? throw new ApplicationException($"Unable to instantiate type {typeof(ILocalFileRepository)}");
            var actionRepository = serviceProvider.GetService<IFileActionRepository>() ?? throw new ApplicationException($"Unable to instantiate type {typeof(IFileActionRepository)}");

            using var logScope = logger.BeginScope("HashFileBlock");

            try
            {
                if (localFile.Ed2kHash != null)
                {
                    return localFile;
                }

                string filename = Path.GetFileName(localFile.Path);

                if (localFile.Ed2kHash != null)
                {
                    logger.LogDebug("File \"{FilePath}\" already hashed. Skipping hashing process...", localFile.Path);
                }
                else
                {
                    var hashAction = new FileAction { Type = FileActionType.Hash, Success = false, FileId = localFile.Id };
                    if (!config.Debug)
                    {
                        await AniSortContext.DatabaseLock.WaitAsync();
                        try
                        {
                            await actionRepository.AddAsync(hashAction);
                            await actionRepository.SaveChangesAsync();
                        }
                        finally
                        {
                            AniSortContext.DatabaseLock.Release();
                        }
                    }

                    await using var fs = new BufferedStream(File.OpenRead(localFile.Path!));
                    long totalBytes;
                    localFile.FileLength = totalBytes = fs.Length;
                    localFile.UpdatedAt = DateTimeOffset.Now;
                    if (!config.Debug)
                    {
                        await AniSortContext.DatabaseLock.WaitAsync();
                        try
                        {
                            await localFileRepository.SaveChangesAsync();
                        }
                        finally
                        {
                            AniSortContext.DatabaseLock.Release();
                        }
                    }

                    onNewHashStarted(localFile.Path, localFile.FileLength);

                    var sw = Stopwatch.StartNew();

                    localFile.Ed2kHash = await Ed2k.HashMultiAsync(fs, new Progress<long>(onProgressUpdate));
                    localFile.Status = ImportStatus.Hashed;
                    localFile.UpdatedAt = DateTimeOffset.Now;
                    hashAction.Success = true;
                    hashAction.Info = $"Successfully hashed file with hash of {localFile.Ed2kHash.ToHexString()}";
                    hashAction.UpdatedAt = DateTimeOffset.Now;

                    onHashFinished();
                    if (!config.Debug)
                    {
                        await AniSortContext.DatabaseLock.WaitAsync();
                        try
                        {
                            await localFileRepository.SaveChangesAsync();
                        }
                        finally
                        {
                            AniSortContext.DatabaseLock.Release();
                        }
                    }

                    sw.Stop();

                    if (EnvironmentHelpers.IsConsolePresent)
                    {
                        Console.Write("\r");
                    }

                    if (EnvironmentHelpers.IsConsolePresent)
                    {
                        logger!.LogInformation("Hashed: {TruncatedFilename}", (localFile.Path.Length + 8 > Console.WindowWidth ? filename : localFile.Path).Truncate(Console.WindowWidth));
                    }
                    else
                    {
                        logger!.LogInformation("Hashed: {Filename}", localFile.Path);
                    }
                    logger.LogDebug("  eD2k hash: {HashInHex}", localFile.Ed2kHash.ToHexString());

                    if (config.Verbose)
                    {
                        logger.LogTrace(
                            "  Processed {SizeInMB:###,###,##0.00}MB in {ElapsedTime} at a rate of {HashRate:F2}MB/s", (double)totalBytes / 1024 / 1024, sw.Elapsed,
                            Math.Round((double)totalBytes / 1024 / 1024 / sw.Elapsed.TotalSeconds));
                    }
                }

                return localFile;
            }
            catch (AniDbConnectionRefusedException ex)
            {
                logger.LogCritical(ex, "AniDB connection timed out. Please wait or switch to a different IP address");
                Environment.Exit(ExitCodes.AniDbConnectionRefused);
                return default;
            }
            catch (DbUpdateConcurrencyException ex)
            {
                foreach (var entry in ex.Entries)
                {
                    logger.LogError(ex, "An issue occurred while trying to update the entity {Entity}", entry);
                }
                return default;
            }
            catch (Exception ex)
            {
                // ReSharper disable once TemplateIsNotCompileTimeConstantProblem
                logger.LogError(ex, ex.Message);
                return default;
            }
        }, options);
    }

    /// <summary>
    /// Build a transform block to return null for files that are either cooling down or have already hit the check limit
    /// </summary>
    /// <param name="options">Optional dataflow execution options</param>
    /// <returns></returns>
    public TransformBlock<LocalFile, LocalFile> BuildFilterCoolingDownFilesBlock(ExecutionDataflowBlockOptions options = null)
    {
        options ??= new ExecutionDataflowBlockOptions();

        return new TransformBlock<LocalFile, LocalFile>(async localFile =>
        {
            var actionRepository = serviceProvider.GetService<IFileActionRepository>() ?? throw new ApplicationException($"Unable to instantiate type {typeof(IFileActionRepository)}");
            var logger = serviceProvider.GetService<ILogger<BlockProvider>>() ?? throw new ApplicationException($"Unable to instantiate type {typeof(ILogger)}");

            using var logScope = logger.BeginScope("FilterCoolingDownFilesBlock");

            try
            {
                string filename = Path.GetFileName(localFile.Path);

                List<FileAction> fileActions;
                await AniSortContext.DatabaseLock.WaitAsync();
                try
                {
                    fileActions = actionRepository!.GetForFile(localFile.Id).ToList().OrderBy(a => a.CreatedAt).ToList();
                }
                finally
                {
                    AniSortContext.DatabaseLock.Release();
                }

                if (config.AniDb.MaxFileSearchRetries.HasValue && fileActions.Count(a => a.Type == FileActionType.Search) >= config.AniDb.MaxFileSearchRetries)
                {
                    if (EnvironmentHelpers.IsConsolePresent)
                    {
                        logger!.LogDebug("File {TruncatedFilename} has hit the retry limit, skipping",
                            (localFile!.Path!.Length + 40 > Console.WindowWidth ? filename : localFile.Path).Truncate(Console.WindowWidth));
                    }
                    else
                    {
                        logger!.LogDebug("File {Filename} has hit the retry limit, skipping", localFile.Path);
                    }
                    return null;
                }

                var lastSearchAction = fileActions.LastOrDefault(a => a.Type == FileActionType.Search);

                if (config.AniDb.FileSearchCooldown != TimeSpan.Zero && (lastSearchAction?.IsCoolingDown(config.AniDb.FileSearchCooldown) ?? false))
                {
                    if (EnvironmentHelpers.IsConsolePresent)
                    {
                        logger!.LogDebug("File {TruncatedFilename} is still cooling down from last search, skipping",
                            (localFile!.Path!.Length + 49 + 5 > Console.WindowWidth ? filename : localFile.Path).Truncate(Console.WindowWidth));
                    }
                    else
                    {
                        logger!.LogDebug("File {Filename} is still cooling down from last search, skipping", localFile.Path);
                    }
                    return null;
                }

                return localFile;
            }
            catch (AniDbConnectionRefusedException ex)
            {
                logger.LogCritical(ex, "AniDB connection timed out. Please wait or switch to a different IP address");
                Environment.Exit(ExitCodes.AniDbConnectionRefused);
                return default;
            }
            catch (DbUpdateConcurrencyException ex)
            {
                foreach (var entry in ex.Entries)
                {
                    logger.LogError(ex, "An issue occurred while trying to update the entity {Entity}", entry);
                }
                return default;
            }
            catch (Exception ex)
            {
                // ReSharper disable once TemplateIsNotCompileTimeConstantProblem
                logger.LogError(ex, ex.Message);
                return default;
            }
        }, options);
    }

    /// <summary>
    /// Build a transform block to search for file info with AniDb
    /// </summary>
    /// <param name="client">AniDbClient managed externally by the caller</param>
    /// <param name="options">Optional dataflow execution options</param>
    /// <returns></returns>
    /// <exception cref="ApplicationException">Thrown when dependencies aren't instantiable via the IoC container</exception>
    public TransformBlock<LocalFile, (LocalFile LocalFile, FileAnimeInfo FileAnimeInfo, FileInfo FileInfo, VideoResolution Resolution)> BuildSearchFileBlock(AniDbClient client,
        ExecutionDataflowBlockOptions options = null)
    {
        options ??= new ExecutionDataflowBlockOptions();

        // Annoying, but for some reason it doesn't infer the type correctly so we need to wrap it in a Func
        return new TransformBlock<LocalFile, (LocalFile LocalFile, FileAnimeInfo FileAnimeInfo, FileInfo FileInfo, VideoResolution Resolution)>(
            async localFile =>
            {
                var logger = serviceProvider.GetService<ILogger<BlockProvider>>() ?? throw new ApplicationException($"Unable to instantiate type {typeof(ILogger)}");
                var actionRepository = serviceProvider.GetService<IFileActionRepository>() ?? throw new ApplicationException($"Unable to instantiate type {typeof(IFileActionRepository)}");
                var localFileRepository = serviceProvider.GetService<ILocalFileRepository>() ?? throw new ApplicationException($"Unable to instantiate type {typeof(ILocalFileRepository)}");
                var animeRepository = serviceProvider.GetService<IAnimeRepository>() ?? throw new ApplicationException($"Unable to instantiate type {typeof(IAnimeRepository)}");
                var episodeRepository = serviceProvider.GetService<IEpisodeRepository>() ?? throw new ApplicationException($"Unable to instantiate type {typeof(IEpisodeRepository)}");
                var releaseGroupRepository = serviceProvider.GetService<IReleaseGroupRepository>() ?? throw new ApplicationException($"Unable to instantiate type {typeof(IReleaseGroupRepository)}");
                var episodeFileRepository = serviceProvider.GetService<IEpisodeFileRepository>() ?? throw new ApplicationException($"Unable to instantiate type {typeof(IEpisodeFileRepository)}");
                var pathBuilderRepository = serviceProvider.GetService<IPathBuilderRepository>() ?? throw new ApplicationException($"Unable to instantiate type {typeof(PathBuilderRepository)}");

                using var logScope = logger.BeginScope("SearchFileBlock");

                try
                {
                    string filename = Path.GetFileName(localFile.Path);

                    var searchAction = new FileAction { Type = FileActionType.Search, Success = false, FileId = localFile.Id };

                    await AniSortContext.DatabaseLock.WaitAsync();
                    try
                    {
                        await actionRepository.AddAsync(searchAction);
                        await actionRepository.SaveChangesAsync();
                    }
                    finally
                    {
                        AniSortContext.DatabaseLock.Release();
                    }

                    var pathBuilder = pathBuilderRepository.GetPathBuilderForPath(localFile.Path);

                    var result = await client.SearchForFile(localFile.FileLength, localFile.Ed2kHash, pathBuilder.FileMask, pathBuilder.AnimeMask);

                    if (!result.FileFound)
                    {
                        await AniSortContext.DatabaseLock.WaitAsync();
                        try
                        {
                            searchAction.Info = "No file found for hash";
                            searchAction.UpdatedAt = DateTimeOffset.Now;
                            await localFileRepository.SaveChangesAsync();
                        }
                        finally
                        {
                            AniSortContext.DatabaseLock.Release();
                        }

                        if (EnvironmentHelpers.IsConsolePresent)
                        {
                            // ReSharper disable once TemplateIsNotCompileTimeConstantProblem
                            logger.LogWarning($"No file found for {filename}".Truncate(Console.WindowWidth));
                        }
                        else
                        {
                            logger.LogWarning("No file found for {FilePath}", filename);
                        }

                        await AniSortContext.DatabaseLock.WaitAsync();
                        try
                        {
                            localFile.Status = ImportStatus.NoFileFound;
                            localFile.UpdatedAt = DateTimeOffset.Now;
                            await localFileRepository.SaveChangesAsync();
                        }
                        finally
                        {
                            AniSortContext.DatabaseLock.Release();
                        }
                        return default;
                    }

                    await AniSortContext.DatabaseLock.WaitAsync();
                    try
                    {
                        searchAction.Success = true;
                        searchAction.Info = $"Found file {result.FileInfo.FileId} for file hash {localFile.Ed2kHash.ToHexString()}";
                        searchAction.UpdatedAt = DateTimeOffset.Now;
                        await actionRepository.SaveChangesAsync();
                        await localFileRepository.SaveChangesAsync();
                    }
                    finally
                    {
                        AniSortContext.DatabaseLock.Release();
                    }

                    // ReSharper disable once TemplateIsNotCompileTimeConstantProblem
                    logger.LogInformation($"File found for {filename}");

                    if (config.Verbose)
                    {
                        logger.LogTrace("  Anime: {AnimeNameInRomaji}", result.AnimeInfo.RomajiName);
                        logger.LogTrace("  Episode: {EpisodeNumber:##} {EpisodeName}", result.AnimeInfo.EpisodeNumber, result.AnimeInfo.EpisodeName);
                        logger.LogTrace("  CRC32: {Crc32Hash}", result.FileInfo.Crc32Hash.ToHexString());
                        logger.LogTrace("  Group: {SubGroupName}", result.AnimeInfo.GroupShortName);
                    }

                    await AniSortContext.DatabaseLock.WaitAsync();
                    try
                    {
                        var (anime, episode, episodeFile, releaseGroup) = await animeRepository.MergeSertAsync(result, false);
                        await animeRepository.SaveChangesAsync();
                        if (!await episodeRepository.ExistsAsync(episode.Id))
                        {
                            episode.AnimeId = anime.Id;
                            await episodeRepository.AddAsync(episode);
                            await episodeRepository.SaveChangesAsync();
                        }
                        if (releaseGroup != null && !await releaseGroupRepository.ExistsForShortNameAsync(releaseGroup.ShortName))
                        {
                            await releaseGroupRepository.AddAsync(releaseGroup);
                            await releaseGroupRepository.SaveChangesAsync();
                            episodeFile.GroupId = releaseGroup.Id;
                        }
                        if (!await episodeFileRepository.ExistsAsync(episodeFile.Id))
                        {
                            episodeFile.EpisodeId = episode.Id;
                            await episodeFileRepository.AddAsync(episodeFile);
                            await episodeFileRepository.SaveChangesAsync();
                        }
                        localFile.EpisodeFileId = episodeFile.Id;
                        await localFileRepository.SaveChangesAsync();
                    }
                    finally
                    {
                        AniSortContext.DatabaseLock.Release();
                    }

                    var resolution = !string.IsNullOrWhiteSpace(result.FileInfo.VideoResolution) ? result.FileInfo.VideoResolution.ParseVideoResolution() : null;

                    if (resolution?.Width == 0 || resolution?.Height == 0)
                    {
                        resolution = null;
                    }

                    return (localFile, result.AnimeInfo, result.FileInfo, resolution);
                }
                catch (AniDbConnectionRefusedException ex)
                {
                    // ReSharper disable once LogMessageIsSentenceProblem
                    logger.LogCritical(ex, "AniDB connection timed out. Please wait or switch to a different IP address.");
                    Environment.Exit(ExitCodes.AniDbConnectionRefused);
                    return default;
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    foreach (var entry in ex.Entries)
                    {
                        logger.LogError(ex, "An issue occurred while trying to update the entity {Entity}", entry);
                    }
                    return default;
                }
                catch (Exception ex)
                {
                    // ReSharper disable once TemplateIsNotCompileTimeConstantProblem
                    logger.LogError(ex, ex.Message);
                    return default;
                }
            }, options);
    }

    /// <summary>
    /// Build a transform block to get the video resolution for a file
    /// </summary>
    /// <param name="options">Optional dataflow execution options</param>
    /// <returns></returns>
    /// <exception cref="ApplicationException">Thrown when dependencies aren't instantiable via the IoC container</exception>
    public TransformBlock<(LocalFile LocalFile, FileAnimeInfo FileAnimeInfo, FileInfo FileInfo, VideoResolution Resolution), (LocalFile LocalFile, FileAnimeInfo FileAnimeInfo, FileInfo FileInfo,
            VideoResolution Resolution)>
        BuildGetFileVideoResolutionBlock(ExecutionDataflowBlockOptions options = null)
    {
        options ??= new ExecutionDataflowBlockOptions();

        return new TransformBlock<(LocalFile LocalFile, FileAnimeInfo FileAnimeInfo, FileInfo FileInfo, VideoResolution Resolution), (LocalFile LocalFile, FileAnimeInfo FileAnimeInfo, FileInfo FileInfo,
            VideoResolution Resolution)>(
            async tuple =>
            {
                var logger = serviceProvider.GetService<ILogger<BlockProvider>>() ?? throw new ApplicationException($"Unable to instantiate type {typeof(ILogger)}");

                using var logScope = logger.BeginScope("GetFileVideoResolutionBlock");

                try
                {
                    var (localFile, animeInfo, fileInfo, resolution) = tuple;

                    if (!fileInfo.HasResolution)
                    {
                        var mediaInfo = await FFProbe.AnalyseAsync(localFile.Path);

                        if (mediaInfo.PrimaryVideoStream == null)
                        {
                            return (localFile, animeInfo, fileInfo, null);
                        }

                        resolution = new VideoResolution(mediaInfo.PrimaryVideoStream.Width, mediaInfo.PrimaryVideoStream.Height);
                    }

                    return (localFile, animeInfo, fileInfo, resolution);
                }
                catch (Exception ex)
                {
                    // ReSharper disable once TemplateIsNotCompileTimeConstantProblem
                    logger.LogError(ex, ex.Message);
                    return default;
                }
            }, options);
    }

    /// <summary>
    /// Build a transform block to rename the file
    /// </summary>
    /// <exception cref="ApplicationException">Thrown when dependencies aren't instantiable via the IoC container</exception>
    /// <returns></returns>
    /// <exception cref="ApplicationException">Thrown when dependencies aren't instantiable via the IoC container</exception>
    public ActionBlock<(LocalFile LocalFile, FileAnimeInfo AnimeInfo, FileInfo FileInfo, VideoResolution Resolution)> BuildRenameFileBlock(ExecutionDataflowBlockOptions options = null)
    {
        options ??= new ExecutionDataflowBlockOptions();

        return new ActionBlock<(LocalFile LocalFile, FileAnimeInfo AnimeInfo, FileInfo FileInfo, VideoResolution Resolution)>(async (tuple) =>
        {
            var actionRepository = serviceProvider.GetService<IFileActionRepository>() ?? throw new ApplicationException($"Unable to instantiate type {typeof(IFileActionRepository)}");
            var localFileRepository = serviceProvider.GetService<ILocalFileRepository>() ?? throw new ApplicationException($"Unable to instantiate type {typeof(ILocalFileRepository)}");
            var logger = serviceProvider.GetService<ILogger<BlockProvider>>() ?? throw new ApplicationException($"Unable to instantiate type {typeof(ILogger)}");
            var pathBuilderRepository = serviceProvider.GetService<IPathBuilderRepository>() ?? throw new ApplicationException($"Unable to instantiate type {typeof(PathBuilderRepository)}");

            using var logScope = logger.BeginScope("RenameFileBlock");

            var (localFile, animeInfo, fileInfo, resolution) = tuple;

            try
            {
                string filename = Path.GetFileName(localFile.Path);
                string extension = Path.GetExtension(filename);
                if (extension == null)
                {
                    throw new ApplicationException($"Video file {localFile.Path} has no extension");
                }
                var pathBuilder = pathBuilderRepository.GetPathBuilderForPath(localFile.Path);

                // Trailing dot is there to prevent Path.ChangeExtension from screwing with the path if it has been ellipsized or has ellipsis in it
                var destinationPathWithoutExtension = pathBuilder.BuildPath(fileInfo, animeInfo,
                    PlatformUtils.MaxPathLength - extension.Length, resolution);

                var destinationPath = destinationPathWithoutExtension + extension;
                var destinationDirectory = Path.GetDirectoryName(destinationPathWithoutExtension);

                if (!config.Debug && !Directory.Exists(destinationDirectory))
                {
                    try
                    {
                        Directory.CreateDirectory(destinationDirectory!);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex,
                            "An unknown error occurred while trying to created the directory. Please make sure the program has access to the target directory");

                        await AniSortContext.DatabaseLock.WaitAsync();
                        try
                        {
                            localFile.Status = ImportStatus.Error;
                            localFile.UpdatedAt = DateTimeOffset.Now;
                            await localFileRepository.SaveChangesAsync();
                        }
                        finally
                        {
                            AniSortContext.DatabaseLock.Release();
                        }
                        return;
                    }
                }

                if (File.Exists(destinationPath))
                {
                    await AniSortContext.DatabaseLock.WaitAsync();
                    try
                    {
                        localFile.Status = fileInfo.HasResolution ? ImportStatus.Imported : ImportStatus.ImportedMissingData;
                        localFile.UpdatedAt = DateTimeOffset.Now;
                        await actionRepository.AddAsync(new FileAction { Type = FileActionType.Copied, Success = true, Info = $"File already exists at {destinationPath}", FileId = localFile.Id });
                        await actionRepository.SaveChangesAsync();
                    }
                    finally
                    {
                        AniSortContext.DatabaseLock.Release();
                    }
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
                    await AniSortContext.DatabaseLock.WaitAsync();
                    try
                    {
                        await localFileRepository.SaveChangesAsync();
                    }
                    finally
                    {
                        AniSortContext.DatabaseLock.Release();
                    }
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

                            File.Copy(localFile.Path!, destinationPath);
                        }
                        catch (UnauthorizedAccessException ex)
                        {
                            logger.LogError("You do not have access to the destination path. Please ensure your user account has access to the destination folder");

                            await AniSortContext.DatabaseLock.WaitAsync();
                            try
                            {
                                localFile.Status = ImportStatus.Error;
                                localFile.UpdatedAt = DateTimeOffset.Now;
                                await localFileRepository.SaveChangesAsync();
                                await actionRepository.AddAsync(new FileAction
                                    { Type = config.Copy ? FileActionType.Copy : FileActionType.Move, Success = false, Exception = $"{ex.Message}\n{ex.StackTrace}", FileId = localFile.Id });
                                await actionRepository.SaveChangesAsync();
                            }
                            finally
                            {
                                AniSortContext.DatabaseLock.Release();
                            }
                            return;
                        }
                        catch (PathTooLongException ex)
                        {
                            logger.LogError(
                                "Filename too long. Yell at Lorathas to fix path length checking if this keeps occurring");

                            await AniSortContext.DatabaseLock.WaitAsync();
                            try
                            {
                                localFile.Status = ImportStatus.Error;
                                localFile.UpdatedAt = DateTimeOffset.Now;
                                await localFileRepository.SaveChangesAsync();
                                await actionRepository.AddAsync(new FileAction
                                    { Type = config.Copy ? FileActionType.Copy : FileActionType.Move, Success = false, Exception = $"{ex.Message}\n{ex.StackTrace}", FileId = localFile.Id });
                                await actionRepository.SaveChangesAsync();
                            }
                            finally
                            {
                                AniSortContext.DatabaseLock.Release();
                            }
                            return;
                        }
                        catch (IOException ex)
                        {
                            logger.LogError(ex, "An unhandled I/O error has occurred");

                            await AniSortContext.DatabaseLock.WaitAsync();
                            try
                            {
                                localFile.Status = ImportStatus.Error;
                                localFile.UpdatedAt = DateTimeOffset.Now;
                                await localFileRepository.SaveChangesAsync();
                                await actionRepository.AddAsync(new FileAction
                                    { Type = config.Copy ? FileActionType.Copy : FileActionType.Move, Success = false, Exception = $"{ex.Message}\n{ex.StackTrace}", FileId = localFile.Id });
                                await actionRepository.SaveChangesAsync();
                            }
                            finally
                            {
                                AniSortContext.DatabaseLock.Release();
                            }
                            return;
                        }

                        await AniSortContext.DatabaseLock.WaitAsync();
                        try
                        {
                            localFile.Status = fileInfo.HasResolution ? ImportStatus.Imported : ImportStatus.ImportedMissingData;
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
                        finally
                        {
                            AniSortContext.DatabaseLock.Release();
                        }
                    }

                    logger.LogInformation("Copied {SourceFilePath} to {DestinationFilePath}", filename, destinationPath);
                }
                else
                {
                    if (!config.Debug)
                    {
                        try
                        {
                            File.Move(localFile.Path!, destinationPath);
                        }
                        catch (UnauthorizedAccessException ex)
                        {
                            logger.LogError(ex,
                                "You do not have access to the destination path. Please ensure your user account has access to the destination folder");

                            await AniSortContext.DatabaseLock.WaitAsync();
                            try
                            {
                                localFile.Status = ImportStatus.Error;
                                localFile.UpdatedAt = DateTimeOffset.Now;
                                await localFileRepository.SaveChangesAsync();
                                await actionRepository.AddAsync(new FileAction
                                    { Type = config.Copy ? FileActionType.Copy : FileActionType.Move, Success = false, Exception = $"{ex.Message}\n{ex.StackTrace}", FileId = localFile.Id });
                                await actionRepository.SaveChangesAsync();
                            }
                            finally
                            {
                                AniSortContext.DatabaseLock.Release();
                            }
                            return;
                        }
                        catch (PathTooLongException ex)
                        {
                            logger.LogError(ex,
                                "Filename too long. Yell at Lorathas to implement path length checking if this keeps occurring");

                            await AniSortContext.DatabaseLock.WaitAsync();
                            try
                            {
                                localFile.Status = ImportStatus.Error;
                                localFile.UpdatedAt = DateTimeOffset.Now;
                                await localFileRepository.SaveChangesAsync();
                                await actionRepository.AddAsync(new FileAction
                                    { Type = config.Copy ? FileActionType.Copy : FileActionType.Move, Success = false, Exception = $"{ex.Message}\n{ex.StackTrace}", FileId = localFile.Id });
                                await actionRepository.SaveChangesAsync();
                            }
                            finally
                            {
                                AniSortContext.DatabaseLock.Release();
                            }
                            return;
                        }
                        catch (IOException ex)
                        {
                            logger.LogError(ex, "An unhandled I/O error has occurred");

                            await AniSortContext.DatabaseLock.WaitAsync();
                            try
                            {
                                localFile.Status = ImportStatus.Error;
                                localFile.UpdatedAt = DateTimeOffset.Now;
                                await localFileRepository.SaveChangesAsync();
                                await actionRepository.AddAsync(new FileAction
                                    { Type = config.Copy ? FileActionType.Copy : FileActionType.Move, Success = false, Exception = $"{ex.Message}\n{ex.StackTrace}", FileId = localFile.Id });
                                await actionRepository.SaveChangesAsync();
                            }
                            finally
                            {
                                AniSortContext.DatabaseLock.Release();
                            }
                            return;
                        }

                        await AniSortContext.DatabaseLock.WaitAsync();
                        try
                        {
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
                        finally
                        {
                            AniSortContext.DatabaseLock.Release();
                        }
                    }

                    logger.LogInformation("Moved {SourceFilePath} to {DestinationFilePath}", filename, destinationPath);
                }
            }
            catch (AniDbConnectionRefusedException ex)
            {
                logger.LogCritical(ex, "AniDB connection timed out. Please wait or switch to a different IP address");
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
                // ReSharper disable once TemplateIsNotCompileTimeConstantProblem
                logger.LogError(ex, ex.Message);
            }
        }, options);
    }
}
