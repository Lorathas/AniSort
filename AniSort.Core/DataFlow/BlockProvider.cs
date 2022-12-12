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
    public TransformBlock<string, FileImport> BuildFetchLocalFileBlock(ExecutionDataflowBlockOptions? options = null)
    {
        options ??= new ExecutionDataflowBlockOptions();

        return new TransformBlock<string, FileImport>(async path =>
        {
            var logger = serviceProvider.GetService<ILogger<BlockProvider>>() ??
                         throw new ApplicationException($"Unable to instantiate type {typeof(ILogger)}");
            var localFileRepository = serviceProvider.GetService<ILocalFileRepository>() ??
                                      throw new ApplicationException(
                                          $"Unable to instantiate type {typeof(ILocalFileRepository)}");

            using var logScope = logger.BeginScope("FetchLocalFileBlock");

            try
            {
                LocalFile? localFile;
                await AniSortContext.DatabaseLock.WaitAsync();
                try
                {
                    localFile = await localFileRepository.GetForPathAsync(path);
                }
                finally
                {
                    AniSortContext.DatabaseLock.Release();
                }

                if (localFile == null)
                {
                    localFile = new LocalFile { Path = path, Status = ImportStatus.NotYetImported };
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
                    logger.LogDebug("File \"{FilePath}\" has already been imported. Skipping...", path);

                    return new FileImport(FileImportState.Failed, localFile);
                }

                return new FileImport(FileImportState.Nominal, localFile);
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

                return new FileImport(FileImportState.Failed, default!);
            }
            catch (Exception ex)
            {
                // ReSharper disable once TemplateIsNotCompileTimeConstantProblem
                logger.LogError(ex, ex.Message);
                return new FileImport(FileImportState.Failed, default!);
            }
        }, options);
    }

    /// <summary>
    /// Build a transform block to hash a file
    /// </summary>
    /// <param name="onNewHashStarted"></param>
    /// <param name="onProgressUpdate">Progress update function to call when hashing</param>
    /// <param name="onHashFinished"></param>
    /// <param name="options">Optional dataflow execution options</param>
    /// <returns></returns>
    public TransformBlock<FileImport, FileImport> BuildHashFileBlock(Action<string, long> onNewHashStarted,
        Action<long> onProgressUpdate, Action onHashFinished, ExecutionDataflowBlockOptions? options = null)
    {
        options ??= new ExecutionDataflowBlockOptions();

        return new TransformBlock<FileImport, FileImport>(async import =>
        {
            var logger = serviceProvider.GetService<ILogger<BlockProvider>>() ??
                         throw new ApplicationException($"Unable to instantiate type {typeof(ILogger)}");
            var localFileRepository = serviceProvider.GetService<ILocalFileRepository>() ??
                                      throw new ApplicationException(
                                          $"Unable to instantiate type {typeof(ILocalFileRepository)}");
            var actionRepository = serviceProvider.GetService<IFileActionRepository>() ??
                                   throw new ApplicationException(
                                       $"Unable to instantiate type {typeof(IFileActionRepository)}");

            using var logScope = logger.BeginScope("HashFileBlock");

            try
            {
                if (import.LocalFile.Ed2kHash != null)
                {
                    return import;
                }

                string filename = Path.GetFileName(import.LocalFile.Path);

                if (import.LocalFile.Ed2kHash != null)
                {
                    logger.LogDebug("File \"{FilePath}\" already hashed. Skipping hashing process...", import.LocalFile.Path);
                }
                else
                {
                    var hashAction = new FileAction
                        { Type = FileActionType.Hash, Success = false, FileId = import.LocalFile.Id };
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

                    await using var fs = new BufferedStream(File.OpenRead(import.LocalFile.Path));
                    long totalBytes;
                    import.LocalFile.FileLength = totalBytes = fs.Length;
                    import.LocalFile.UpdatedAt = DateTimeOffset.Now;
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

                    onNewHashStarted(import.LocalFile.Path, import.LocalFile.FileLength);

                    var sw = Stopwatch.StartNew();

                    import.LocalFile.Ed2kHash = await Ed2k.HashMultiAsync(fs, new Progress<long>(onProgressUpdate));
                    import.LocalFile.Status = ImportStatus.Hashed;
                    import.LocalFile.UpdatedAt = DateTimeOffset.Now;
                    hashAction.Success = true;
                    hashAction.Info = $"Successfully hashed file with hash of {import.LocalFile.Ed2kHash.ToHexString()}";
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
                        logger.LogInformation("Hashed: {TruncatedFilename}",
                            (import.LocalFile.Path.Length + 8 > Console.WindowWidth ? filename : import.LocalFile.Path).Truncate(
                                Console.WindowWidth));
                    }
                    else
                    {
                        logger.LogInformation("Hashed: {Filename}", import.LocalFile.Path);
                    }

                    logger.LogDebug("  eD2k hash: {HashInHex}", import.LocalFile.Ed2kHash.ToHexString());

                    if (config.Verbose)
                    {
                        logger.LogTrace(
                            "  Processed {SizeInMB:###,###,##0.00}MB in {ElapsedTime} at a rate of {HashRate:F2}MB/s",
                            (double)totalBytes / 1024 / 1024, sw.Elapsed,
                            Math.Round((double)totalBytes / 1024 / 1024 / sw.Elapsed.TotalSeconds));
                    }
                }

                return import;
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

                return import with { State = FileImportState.Failed };
            }
            catch (Exception ex)
            {
                // ReSharper disable once TemplateIsNotCompileTimeConstantProblem
                logger.LogError(ex, ex.Message);
                return import with { State = FileImportState.Failed };
            }
        }, options);
    }

    /// <summary>
    /// Build a transform block to return null for files that are either cooling down or have already hit the check limit
    /// </summary>
    /// <param name="options">Optional dataflow execution options</param>
    /// <returns></returns>
    public TransformBlock<FileImport, FileImport> BuildFilterCoolingDownFilesBlock(
        ExecutionDataflowBlockOptions? options = null)
    {
        options ??= new ExecutionDataflowBlockOptions();

        return new TransformBlock<FileImport, FileImport>(async import =>
        {
            var actionRepository = serviceProvider.GetService<IFileActionRepository>() ??
                                   throw new ApplicationException(
                                       $"Unable to instantiate type {typeof(IFileActionRepository)}");
            var logger = serviceProvider.GetService<ILogger<BlockProvider>>() ??
                         throw new ApplicationException($"Unable to instantiate type {typeof(ILogger)}");

            using var logScope = logger.BeginScope("FilterCoolingDownFilesBlock");

            try
            {
                string filename = Path.GetFileName(import.LocalFile.Path);

                List<FileAction> fileActions;
                await AniSortContext.DatabaseLock.WaitAsync();
                try
                {
                    fileActions = actionRepository.GetForFile(import.LocalFile.Id).ToList().OrderBy(a => a.CreatedAt)
                        .ToList();
                }
                finally
                {
                    AniSortContext.DatabaseLock.Release();
                }

                if (config.AniDb.MaxFileSearchRetries.HasValue &&
                    fileActions.Count(a => a.Type == FileActionType.Search) >= config.AniDb.MaxFileSearchRetries)
                {
                    if (EnvironmentHelpers.IsConsolePresent)
                    {
                        logger.LogDebug("File {TruncatedFilename} has hit the retry limit, skipping",
                            (import.LocalFile.Path.Length + 40 > Console.WindowWidth ? filename : import.LocalFile.Path).Truncate(
                                Console.WindowWidth));
                    }
                    else
                    {
                        logger.LogDebug("File {Filename} has hit the retry limit, skipping", import.LocalFile.Path);
                    }

                    return import with { State = FileImportState.RateLimited };
                }

                var lastSearchAction = fileActions.LastOrDefault(a => a.Type == FileActionType.Search);

                if (config.AniDb.FileSearchCooldown != TimeSpan.Zero &&
                    (lastSearchAction?.IsCoolingDown(config.AniDb.FileSearchCooldown) ?? false))
                {
                    if (EnvironmentHelpers.IsConsolePresent)
                    {
                        logger.LogDebug("File {TruncatedFilename} is still cooling down from last search, skipping",
                            (import.LocalFile.Path.Length + 49 + 5 > Console.WindowWidth ? filename : import.LocalFile.Path)
                            .Truncate(Console.WindowWidth));
                    }
                    else
                    {
                        logger.LogDebug("File {Filename} is still cooling down from last search, skipping", import.LocalFile.Path);
                    }

                    return import with { State = FileImportState.RateLimited };
                }

                return import;
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

                return import with { State = FileImportState.Failed };
            }
            catch (Exception ex)
            {
                // ReSharper disable once TemplateIsNotCompileTimeConstantProblem
                logger.LogError(ex, ex.Message);
                return import with { State = FileImportState.Failed };
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
    public TransformBlock<FileImport, FileImport> BuildSearchFileBlock(AniDbClient client, ExecutionDataflowBlockOptions? options = null)
    {
        options ??= new ExecutionDataflowBlockOptions();

        // Annoying, but for some reason it doesn't infer the type correctly so we need to wrap it in a Func
        return new TransformBlock<FileImport, FileImport>(
            async import =>
            {
                var logger = serviceProvider.GetService<ILogger<BlockProvider>>() ??
                             throw new ApplicationException($"Unable to instantiate type {typeof(ILogger)}");
                var actionRepository = serviceProvider.GetService<IFileActionRepository>() ??
                                       throw new ApplicationException(
                                           $"Unable to instantiate type {typeof(IFileActionRepository)}");
                var localFileRepository = serviceProvider.GetService<ILocalFileRepository>() ??
                                          throw new ApplicationException(
                                              $"Unable to instantiate type {typeof(ILocalFileRepository)}");
                var animeRepository = serviceProvider.GetService<IAnimeRepository>() ??
                                      throw new ApplicationException(
                                          $"Unable to instantiate type {typeof(IAnimeRepository)}");
                var episodeRepository = serviceProvider.GetService<IEpisodeRepository>() ??
                                        throw new ApplicationException(
                                            $"Unable to instantiate type {typeof(IEpisodeRepository)}");
                var releaseGroupRepository = serviceProvider.GetService<IReleaseGroupRepository>() ??
                                             throw new ApplicationException(
                                                 $"Unable to instantiate type {typeof(IReleaseGroupRepository)}");
                var episodeFileRepository = serviceProvider.GetService<IEpisodeFileRepository>() ??
                                            throw new ApplicationException(
                                                $"Unable to instantiate type {typeof(IEpisodeFileRepository)}");
                var pathBuilderRepository = serviceProvider.GetService<IPathBuilderRepository>() ??
                                            throw new ApplicationException(
                                                $"Unable to instantiate type {typeof(PathBuilderRepository)}");

                using var logScope = logger.BeginScope("SearchFileBlock");

                if (import.LocalFile.Ed2kHash == null)
                {
                    logger.LogError("No ED2k Hash found to search with for file {Filename}", import.LocalFile.Path);
                    return import with { State = FileImportState.Failed };
                }

                try
                {
                    string filename = Path.GetFileName(import.LocalFile.Path);

                    var searchAction = new FileAction
                        { Type = FileActionType.Search, Success = false, FileId = import.LocalFile.Id };

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

                    var pathBuilder = pathBuilderRepository.GetPathBuilderForPath(import.LocalFile.Path);

                    var result = await client.SearchForFile(import.LocalFile.FileLength, import.LocalFile.Ed2kHash,
                        pathBuilder.FileMask, pathBuilder.AnimeMask);

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
                            import.LocalFile.Status = ImportStatus.NoFileFound;
                            import.LocalFile.UpdatedAt = DateTimeOffset.Now;
                            await localFileRepository.SaveChangesAsync();
                        }
                        finally
                        {
                            AniSortContext.DatabaseLock.Release();
                        }

                        return import with { State = FileImportState.Failed };
                    }

                    await AniSortContext.DatabaseLock.WaitAsync();
                    try
                    {
                        searchAction.Success = true;
                        searchAction.Info =
                            $"Found file {result.FileInfo.FileId} for file hash {import.LocalFile.Ed2kHash.ToHexString()}";
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
                        logger.LogTrace("  Episode: {EpisodeNumber:##} {EpisodeName}", result.AnimeInfo.EpisodeNumber,
                            result.AnimeInfo.EpisodeName);
                        logger.LogTrace("  CRC32: {Crc32Hash}", result.FileInfo.Crc32Hash.ToHexString());
                        logger.LogTrace("  Group: {SubGroupName}", result.AnimeInfo.GroupShortName);
                    }

                    await AniSortContext.DatabaseLock.WaitAsync();
                    try
                    {
                        var (anime, episode, episodeFile, releaseGroup) =
                            await animeRepository.MergeSertAsync(result, false);
                        await animeRepository.SaveChangesAsync();
                        if (!await episodeRepository.ExistsAsync(episode.Id))
                        {
                            episode.AnimeId = anime.Id;
                            await episodeRepository.AddAsync(episode);
                            await episodeRepository.SaveChangesAsync();
                        }

                        if (!await releaseGroupRepository.ExistsForShortNameAsync(releaseGroup.ShortName!))
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

                        import.LocalFile.EpisodeFileId = episodeFile.Id;
                        await localFileRepository.SaveChangesAsync();
                    }
                    finally
                    {
                        AniSortContext.DatabaseLock.Release();
                    }

                    var resolution = !string.IsNullOrWhiteSpace(result.FileInfo.VideoResolution)
                        ? result.FileInfo.VideoResolution.ParseVideoResolution()
                        : null;

                    if (resolution?.Width == 0 || resolution?.Height == 0)
                    {
                        resolution = null;
                    }

                    return import with { AnimeInfo = result.AnimeInfo, FileInfo = result.FileInfo, Resolution = resolution };
                }
                catch (AniDbConnectionRefusedException ex)
                {
                    // ReSharper disable once LogMessageIsSentenceProblem
                    logger.LogCritical(ex,
                        "AniDB connection timed out. Please wait or switch to a different IP address.");
                    Environment.Exit(ExitCodes.AniDbConnectionRefused);
                    return default;
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    foreach (var entry in ex.Entries)
                    {
                        logger.LogError(ex, "An issue occurred while trying to update the entity {Entity}", entry);
                    }

                    return import with { State = FileImportState.Failed };
                }
                catch (Exception ex)
                {
                    // ReSharper disable once TemplateIsNotCompileTimeConstantProblem
                    logger.LogError(ex, ex.Message);
                    return import with { State = FileImportState.Failed };
                }
            }, options);
    }

    /// <summary>
    /// Build a transform block to get the video resolution for a file
    /// </summary>
    /// <param name="options">Optional dataflow execution options</param>
    /// <returns></returns>
    /// <exception cref="ApplicationException">Thrown when dependencies aren't instantiable via the IoC container</exception>
    public TransformBlock<FileImport, FileImport> BuildGetFileVideoResolutionBlock(
        ExecutionDataflowBlockOptions? options = null)
    {
        options ??= new ExecutionDataflowBlockOptions();

        return new TransformBlock<FileImport, FileImport>(
            async tuple =>
            {
                var logger = serviceProvider.GetService<ILogger<BlockProvider>>() ??
                             throw new ApplicationException($"Unable to instantiate type {typeof(ILogger)}");

                using var logScope = logger.BeginScope("GetFileVideoResolutionBlock");

                try
                {
                    VideoResolution? resolution = tuple.Resolution;

                    if (!tuple.FileInfo?.HasResolution ?? true)
                    {
                        if (!string.IsNullOrWhiteSpace(tuple.LocalFile.Path))
                        {
                            logger.LogError("File had no path associated with it");
                            return tuple with { State = FileImportState.Failed };
                        }

                        var mediaInfo = await FFProbe.AnalyseAsync(tuple.LocalFile.Path);

                        if (mediaInfo.PrimaryVideoStream == null)
                        {
                            return tuple with { State = FileImportState.Failed };
                        }

                        resolution = new VideoResolution(mediaInfo.PrimaryVideoStream.Width,
                            mediaInfo.PrimaryVideoStream.Height);
                    }

                    return tuple with { Resolution = resolution };
                }
                catch (Exception ex)
                {
                    // ReSharper disable once TemplateIsNotCompileTimeConstantProblem
                    logger.LogError(ex, ex.Message + ex.StackTrace);
                    return tuple with { State = FileImportState.Failed };
                }
            }, options);
    }

    /// <summary>
    /// Build a transform block to rename the file
    /// </summary>
    /// <exception cref="ApplicationException">Thrown when dependencies aren't instantiable via the IoC container</exception>
    /// <returns></returns>
    public ActionBlock<FileImport> BuildRenameFileBlock(ExecutionDataflowBlockOptions? options = null)
    {
        options ??= new ExecutionDataflowBlockOptions();

        return new ActionBlock<FileImport>(async (tuple) =>
        {
            var actionRepository = serviceProvider.GetService<IFileActionRepository>() ??
                                   throw new ApplicationException(
                                       $"Unable to instantiate type {typeof(IFileActionRepository)}");
            var localFileRepository = serviceProvider.GetService<ILocalFileRepository>() ??
                                      throw new ApplicationException(
                                          $"Unable to instantiate type {typeof(ILocalFileRepository)}");
            var logger = serviceProvider.GetService<ILogger<BlockProvider>>() ??
                         throw new ApplicationException($"Unable to instantiate type {typeof(ILogger)}");
            var pathBuilderRepository = serviceProvider.GetService<IPathBuilderRepository>() ??
                                        throw new ApplicationException(
                                            $"Unable to instantiate type {typeof(PathBuilderRepository)}");

            using var logScope = logger.BeginScope("RenameFileBlock");

            var localFile = tuple.LocalFile;
            var animeInfo = tuple.AnimeInfo;
            var fileInfo = tuple.FileInfo;
            var resolution = tuple.Resolution;

            if (string.IsNullOrWhiteSpace(localFile.Path))
            {
                logger.LogError("Path cannot be empty");
                return;
            }

            if (animeInfo == null)
            {
                logger.LogError("Cannot sort file without anime info");
                return;
            }

            if (fileInfo == null)
            {
                logger.LogError("Cannot sort file without file info");
                return;
            }

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
                    PlatformUtils.MaxPathLength - extension.Length, resolution ?? new VideoResolution(0, 0));

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
                        localFile.Status = fileInfo.HasResolution
                            ? ImportStatus.Imported
                            : ImportStatus.ImportedMissingData;
                        localFile.UpdatedAt = DateTimeOffset.Now;
                        await actionRepository.AddAsync(new FileAction
                        {
                            Type = FileActionType.Copied, Success = true,
                            Info = $"File already exists at {destinationPath}", FileId = localFile.Id
                        });
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
                            FileActions = new List<FileAction>
                            {
                                new()
                                {
                                    Type = FileActionType.Copied, Success = true,
                                    Info = $"File already exists at {destinationPath}"
                                }
                            }
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

                    logger.LogDebug("Destination file \"{DestinationPath}\" already exists. Skipping...",
                        destinationPath);
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

                            File.Copy(localFile.Path, destinationPath);
                        }
                        catch (UnauthorizedAccessException ex)
                        {
                            logger.LogError(
                                "You do not have access to the destination path. Please ensure your user account has access to the destination folder");

                            await AniSortContext.DatabaseLock.WaitAsync();
                            try
                            {
                                localFile.Status = ImportStatus.Error;
                                localFile.UpdatedAt = DateTimeOffset.Now;
                                await localFileRepository.SaveChangesAsync();
                                await actionRepository.AddAsync(new FileAction
                                {
                                    Type = config.Copy ? FileActionType.Copy : FileActionType.Move, Success = false,
                                    Exception = $"{ex.Message}\n{ex.StackTrace}", FileId = localFile.Id
                                });
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
                                {
                                    Type = config.Copy ? FileActionType.Copy : FileActionType.Move, Success = false,
                                    Exception = $"{ex.Message}\n{ex.StackTrace}", FileId = localFile.Id
                                });
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
                                {
                                    Type = config.Copy ? FileActionType.Copy : FileActionType.Move, Success = false,
                                    Exception = $"{ex.Message}\n{ex.StackTrace}", FileId = localFile.Id
                                });
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
                            localFile.Status = fileInfo.HasResolution
                                ? ImportStatus.Imported
                                : ImportStatus.ImportedMissingData;
                            await localFileRepository.AddAsync(new LocalFile
                            {
                                Path = localFile.Path,
                                Status = localFile.Status,
                                Ed2kHash = localFile.Ed2kHash,
                                EpisodeFileId = localFile.EpisodeFileId,
                                FileLength = localFile.FileLength,
                                FileActions = new List<FileAction>
                                {
                                    new()
                                    {
                                        Type = FileActionType.Copied, Success = true,
                                        Info = $"Source file copied to {destinationPath}"
                                    }
                                }
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

                    logger.LogInformation("Copied {SourceFilePath} to {DestinationFilePath}", filename,
                        destinationPath);
                }
                else
                {
                    if (!config.Debug)
                    {
                        try
                        {
                            File.Move(localFile.Path, destinationPath);
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
                                {
                                    Type = config.Copy ? FileActionType.Copy : FileActionType.Move, Success = false,
                                    Exception = $"{ex.Message}\n{ex.StackTrace}", FileId = localFile.Id
                                });
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
                                {
                                    Type = config.Copy ? FileActionType.Copy : FileActionType.Move, Success = false,
                                    Exception = $"{ex.Message}\n{ex.StackTrace}", FileId = localFile.Id
                                });
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
                                {
                                    Type = config.Copy ? FileActionType.Copy : FileActionType.Move, Success = false,
                                    Exception = $"{ex.Message}\n{ex.StackTrace}", FileId = localFile.Id
                                });
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

public enum FileImportState
{
    Nominal,
    Failed,
    RateLimited
}

public record FileImport(FileImportState State, LocalFile LocalFile, FileAnimeInfo? AnimeInfo = null,
    FileInfo? FileInfo = null, VideoResolution? Resolution = null);