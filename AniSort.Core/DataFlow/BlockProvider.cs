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
    private readonly IConfigProvider configProvider;

    private readonly IServiceProvider serviceProvider;

    public BlockProvider(IConfigProvider configProvider, IServiceProvider serviceProvider)
    {
        this.configProvider = configProvider;
        this.serviceProvider = serviceProvider;
    }

    /// <summary>
    /// Build a transform block to fetch possible existing local file data, or create new data for a path
    /// </summary>
    /// <param name="options">Optional dataflow execution options</param>
    /// <returns></returns>
    public TransformBlock<MetadataFileJobParams, MetadataFileJobParams> BuildFetchLocalFileBlock(ExecutionDataflowBlockOptions? options = null)
    {
        options ??= new();

        return new TransformBlock<MetadataFileJobParams, MetadataFileJobParams>(async parameters =>
        {
            var logger = serviceProvider.GetService<ILogger<BlockProvider>>() ?? throw new ApplicationException($"Unable to instantiate type {typeof(ILogger)}");
            var localFileRepository = serviceProvider.GetService<ILocalFileRepository>() ?? throw new ApplicationException($"Unable to instantiate type {typeof(ILocalFileRepository)}");
            var jobStepRepository = serviceProvider.GetService<IJobStepRepository>() ?? throw new ApplicationException($"Unable to instantiate type {typeof(IJobStepRepository)}");

            var hashStep = parameters.Job?.Steps.FirstOrDefault(s => s.Type == StepType.Hash);

            JobStep? existingStep = null;
            if (hashStep != null)
            {
                existingStep = await jobStepRepository.GetByIdAsync(hashStep.Id);
            }

            using var logScope = logger.BeginScope("FetchLocalFileBlock");

            try
            {
                LocalFile? localFile;
                await AniSortContext.DatabaseLock.WaitAsync();
                try
                {
                    localFile = await localFileRepository.GetForPathAsync(parameters.FileInfo.FullName);
                }
                finally
                {
                    AniSortContext.DatabaseLock.Release();
                }

                if (localFile == null)
                {
                    localFile = new LocalFile { Path = parameters.FileInfo.FullName, Status = ImportStatus.NotYetImported, EpisodeFile = null };
                    if (!configProvider.Config.Debug)
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
                    logger.LogDebug("File \"{FilePath}\" has already been imported. Skipping...", parameters);

                    return parameters with { Failed = true };
                }

                if (existingStep != null)
                {
                    existingStep.CurrentProgress++;
                    await jobStepRepository.SaveChangesAsync();
                }

                return parameters with { LocalFile = localFile };
            }
            catch (AniDbConnectionRefusedException ex)
            {
                if (existingStep != null)
                {
                    existingStep.Logs.Add(new StepLog(ex, AniDbErrorMessages.AniDbTimeout));
                }

                logger.LogCritical(ex, AniDbErrorMessages.AniDbTimeout);
                Environment.Exit(ExitCodes.AniDbConnectionRefused);
                return parameters with { Failed = true };
            }
            catch (DbUpdateConcurrencyException ex)
            {
                foreach (var entry in ex.Entries)
                {
                    logger.LogError(ex, "An issue occurred while trying to update the entity {Entity}", entry);
                }
                return parameters with { Failed = true };
            }
            catch (Exception ex)
            {
                // ReSharper disable once TemplateIsNotCompileTimeConstantProblem
                logger.LogError(ex, ex.Message);
                return parameters with { Failed = true };
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
    public TransformBlock<MetadataFileJobParams, MetadataFileJobParams> BuildHashFileBlock(Action<string, long> onNewHashStarted, Action<long> onProgressUpdate, Action onHashFinished, ExecutionDataflowBlockOptions? options = null)
    {
        options ??= new();

        return new TransformBlock<MetadataFileJobParams, MetadataFileJobParams>(async parameters =>
        {
            var logger = serviceProvider.GetService<ILogger<BlockProvider>>() ?? throw new ApplicationException($"Unable to instantiate type {typeof(ILogger)}");
            var localFileRepository = serviceProvider.GetService<ILocalFileRepository>() ?? throw new ApplicationException($"Unable to instantiate type {typeof(ILocalFileRepository)}");
            var actionRepository = serviceProvider.GetService<IFileActionRepository>() ?? throw new ApplicationException($"Unable to instantiate type {typeof(IFileActionRepository)}");
            var jobUpdateProvider = serviceProvider.GetService<IJobUpdateProvider>() ?? throw new ApplicationException($"Unable to instantiate type {typeof(IJobUpdateProvider)}");
            var jobStepRepository = serviceProvider.GetService<IJobStepRepository>() ?? throw new ApplicationException($"Unable to instantiate type {typeof(IJobStepRepository)}");

            using var logScope = logger.BeginScope("HashFileBlock");

            try
            {
                if (parameters.LocalFile!.Ed2kHash != null)
                {
                    return parameters;
                }

                string filename = parameters.FileInfo.Name;

                if (parameters.LocalFile!.Ed2kHash != null)
                {
                    logger.LogDebug("File \"{FilePath}\" already hashed. Skipping hashing process...", parameters.LocalFile.Path);
                }
                else
                {
                    var hashAction = new FileAction { Type = FileActionType.Hash, Success = false, FileId = parameters.LocalFile!.Id };
                    if (!configProvider.Config.Debug)
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

                    await using var fs = new BufferedStream(parameters.FileInfo.OpenRead());
                    long totalBytes;
                    parameters.LocalFile.FileLength = totalBytes = fs.Length;
                    parameters.LocalFile.UpdatedAt = DateTimeOffset.Now;
                    if (!configProvider.Config.Debug)
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

                    onNewHashStarted(parameters.LocalFile.Path, parameters.FileInfo.Length);

                    var sw = Stopwatch.StartNew();

                    void OnHashProgress(long progress)
                    {
                        if (parameters.Job != null)
                        {
                        }

                        onProgressUpdate(progress);
                    }

                    ;

                    parameters.LocalFile.Ed2kHash = await Ed2k.HashMultiAsync(fs, new Progress<long>(OnHashProgress));
                    parameters.LocalFile.Status = ImportStatus.Hashed;
                    parameters.LocalFile.UpdatedAt = DateTimeOffset.Now;
                    hashAction.Success = true;
                    hashAction.Info = $"Successfully hashed file with hash of {parameters.LocalFile.Ed2kHash.ToHexString()}";
                    hashAction.UpdatedAt = DateTimeOffset.Now;

                    onHashFinished();
                    if (!configProvider.Config.Debug)
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
                        logger.LogInformation("Hashed: {TruncatedFilename}", (parameters.LocalFile.Path.Length + 8 > Console.WindowWidth ? filename : parameters.LocalFile.Path).Truncate(Console.WindowWidth));
                    }
                    else
                    {
                        logger.LogInformation("Hashed: {Filename}", parameters.LocalFile.Path);
                    }
                    logger.LogDebug("  eD2k hash: {HashInHex}", parameters.LocalFile.Ed2kHash.ToHexString());

                    if (configProvider.Config.Verbose)
                    {
                        logger.LogTrace(
                            "  Processed {SizeInMB:###,###,##0.00}MB in {ElapsedTime} at a rate of {HashRate:F2}MB/s", (double) totalBytes / 1024 / 1024, sw.Elapsed,
                            Math.Round((double) totalBytes / 1024 / 1024 / sw.Elapsed.TotalSeconds));
                    }
                }

                return parameters;
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
                return parameters with {Failed = true};
            }
            catch (Exception ex)
            {
                // ReSharper disable once TemplateIsNotCompileTimeConstantProblem
                logger.LogError(ex, ex.Message);
                return parameters with {Failed = true};
            }
        }, options);
    }

    /// <summary>
    /// Build a transform block to return null for files that are either cooling down or have already hit the check limit
    /// </summary>
    /// <param name="options">Optional dataflow execution options</param>
    /// <returns></returns>
    public TransformBlock<MetadataFileJobParams, MetadataFileJobParams> BuildFilterCoolingDownFilesBlock(ExecutionDataflowBlockOptions? options = null)
    {
        options ??= new();

        return new TransformBlock<MetadataFileJobParams, MetadataFileJobParams>(async parameters =>
        {
            if (parameters.LocalFile == null)
            {
                return parameters with { Failed = true };
            }

            var actionRepository = serviceProvider.GetService<IFileActionRepository>() ?? throw new ApplicationException($"Unable to instantiate type {typeof(IFileActionRepository)}");
            var logger = serviceProvider.GetService<ILogger<BlockProvider>>() ?? throw new ApplicationException($"Unable to instantiate type {typeof(ILogger)}");

            using var logScope = logger.BeginScope("FilterCoolingDownFilesBlock");

            try
            {
                string filename = Path.GetFileName(parameters.LocalFile.Path);

                List<FileAction> fileActions;
                await AniSortContext.DatabaseLock.WaitAsync();
                try
                {
                    fileActions = actionRepository.GetForFile(parameters.LocalFile.Id).ToList().OrderBy(a => a.CreatedAt).ToList();
                }
                finally
                {
                    AniSortContext.DatabaseLock.Release();
                }

                if (configProvider.Config.AniDb.MaxFileSearchRetries.HasValue && fileActions.Count(a => a.Type == FileActionType.Search) >= configProvider.Config.AniDb.MaxFileSearchRetries)
                {
                    if (EnvironmentHelpers.IsConsolePresent)
                    {
                        logger.LogDebug("File {TruncatedFilename} has hit the retry limit, skipping",
                            (parameters.FileInfo.FullName.Length + 40 > Console.WindowWidth ? filename : parameters.FileInfo.FullName).Truncate(Console.WindowWidth));
                    }
                    else
                    {
                        logger!.LogDebug("File {Filename} has hit the retry limit, skipping", parameters.FileInfo.FullName);
                    }
                    return parameters with { IsCoolingDown = true };
                }

                var lastSearchAction = fileActions.LastOrDefault(a => a.Type == FileActionType.Search);

                if (configProvider.Config.AniDb.FileSearchCooldown != TimeSpan.Zero && (lastSearchAction?.IsCoolingDown(configProvider.Config.AniDb.FileSearchCooldown) ?? false))
                {
                    if (EnvironmentHelpers.IsConsolePresent)
                    {
                        logger.LogDebug("File {TruncatedFilename} is still cooling down from last search, skipping",
                            (parameters.LocalFile!.Path.Length + 49 + 5 > Console.WindowWidth ? filename : parameters.LocalFile.Path).Truncate(Console.WindowWidth));
                    }
                    else
                    {
                        logger.LogDebug("File {Filename} is still cooling down from last search, skipping", parameters.LocalFile.Path);
                    }
                    return parameters with { IsCoolingDown = true };
                }

                return parameters;
            }
            catch (AniDbConnectionRefusedException ex)
            {
                logger.LogCritical(ex, "AniDB connection timed out. Please wait or switch to a different IP address");
                Environment.Exit(ExitCodes.AniDbConnectionRefused);
                return parameters with { Failed = true };
            }
            catch (DbUpdateConcurrencyException ex)
            {
                foreach (var entry in ex.Entries)
                {
                    logger.LogError(ex, "An issue occurred while trying to update the entity {Entity}", entry);
                }
                return parameters with { Failed = true };
            }
            catch (Exception ex)
            {
                // ReSharper disable once TemplateIsNotCompileTimeConstantProblem
                logger.LogError(ex, ex.Message);
                return parameters with { Failed = true };
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
    public TransformBlock<MetadataFileJobParams, MetadataFileJobParams> BuildSearchFileBlock(AniDbClient client,
        ExecutionDataflowBlockOptions? options = null)
    {
        options ??= new();

        // Annoying, but for some reason it doesn't infer the type correctly so we need to wrap it in a Func
        return new TransformBlock<MetadataFileJobParams, MetadataFileJobParams>(async parameters =>
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
                string filename = parameters.FileInfo.Name;

                var searchAction = new FileAction { Type = FileActionType.Search, Success = false, FileId = parameters.LocalFile!.Id };

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

                var pathBuilder = pathBuilderRepository.GetPathBuilderForPath(parameters.FileInfo.FullName);

                var result = await client.SearchForFile(parameters.LocalFile.FileLength, parameters.LocalFile.Ed2kHash!, pathBuilder.FileMask, pathBuilder.AnimeMask);

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
                        parameters.LocalFile.Status = ImportStatus.NoFileFound;
                        parameters.LocalFile.UpdatedAt = DateTimeOffset.Now;
                        await localFileRepository.SaveChangesAsync();
                    }
                    finally
                    {
                        AniSortContext.DatabaseLock.Release();
                    }

                    return parameters with { Failed = true };
                }

                await AniSortContext.DatabaseLock.WaitAsync();
                try
                {
                    searchAction.Success = true;
                    searchAction.Info = $"Found file {result.FileInfo.FileId} for file hash {parameters.LocalFile.Ed2kHash!.ToHexString()}";
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

                if (configProvider.Config.Verbose)
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
                    parameters.LocalFile.EpisodeFileId = episodeFile.Id;
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

                return new(parameters.FileInfo, parameters.LocalFile, parameters.IsCoolingDown, result.AnimeInfo, result.FileInfo, resolution, parameters.Job);
            }
            catch (AniDbConnectionRefusedException ex)
            {
                // ReSharper disable once LogMessageIsSentenceProblem
                logger.LogCritical(ex, "AniDB connection timed out. Please wait or switch to a different IP address.");
                Environment.Exit(ExitCodes.AniDbConnectionRefused);
                return new(parameters.FileInfo, parameters.LocalFile, parameters.IsCoolingDown, default, default, default, parameters.Job, true);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                foreach (var entry in ex.Entries)
                {
                    logger.LogError(ex, "An issue occurred while trying to update the entity {Entity}", entry);
                }
                return new(parameters.FileInfo, parameters.LocalFile, parameters.IsCoolingDown, default, default, default, parameters.Job, true);
            }
            catch (Exception ex)
            {
                // ReSharper disable once TemplateIsNotCompileTimeConstantProblem
                logger.LogError(ex, ex.Message);
                return new(parameters.FileInfo, parameters.LocalFile, parameters.IsCoolingDown, default, default, default, parameters.Job, true);
            }
        }, options);
    }

    /// <summary>
    /// Build a transform block to get the video resolution for a file
    /// </summary>
    /// <param name="options">Optional dataflow execution options</param>
    /// <returns></returns>
    /// <exception cref="ApplicationException">Thrown when dependencies aren't instantiable via the IoC container</exception>
    public TransformBlock<MetadataFileJobParams, MetadataFileJobParams>
        BuildGetFileVideoResolutionBlock(ExecutionDataflowBlockOptions? options = null)
    {
        options ??= new();

        return new TransformBlock<MetadataFileJobParams, MetadataFileJobParams>(
            async parameters =>
            {
                var logger = serviceProvider.GetService<ILogger<BlockProvider>>() ?? throw new ApplicationException($"Unable to instantiate type {typeof(ILogger)}");

                using var logScope = logger.BeginScope("GetFileVideoResolutionBlock");

                try
                {
                    var resolution = parameters.VideoResolution;

                    if (!parameters.AnimeFileInfo!.HasResolution)
                    {
                        var mediaInfo = await FFProbe.AnalyseAsync(parameters.FileInfo.FullName);

                        if (mediaInfo.PrimaryVideoStream == null)
                        {
                            return parameters with { Failed = true };
                        }

                        resolution = new VideoResolution(mediaInfo.PrimaryVideoStream.Width, mediaInfo.PrimaryVideoStream.Height);
                    }

                    return parameters with { VideoResolution = resolution };
                }
                catch (Exception ex)
                {
                    // ReSharper disable once TemplateIsNotCompileTimeConstantProblem
                    logger.LogError(ex, ex.Message);
                    return parameters with { Failed = true };
                }
            }, options);
    }

    /// <summary>
    /// Build a transform block to rename the file
    /// </summary>
    /// <exception cref="ApplicationException">Thrown when dependencies aren't instantiable via the IoC container</exception>
    /// <returns></returns>
    /// <exception cref="ApplicationException">Thrown when dependencies aren't instantiable via the IoC container</exception>
    public ActionBlock<MetadataFileJobParams> BuildRenameFileBlock(ExecutionDataflowBlockOptions? options = null)
    {
        options ??= new();

        return new ActionBlock<MetadataFileJobParams>(async parameters =>
        {
            var actionRepository = serviceProvider.GetService<IFileActionRepository>() ?? throw new ApplicationException($"Unable to instantiate type {typeof(IFileActionRepository)}");
            var localFileRepository = serviceProvider.GetService<ILocalFileRepository>() ?? throw new ApplicationException($"Unable to instantiate type {typeof(ILocalFileRepository)}");
            var logger = serviceProvider.GetService<ILogger<BlockProvider>>() ?? throw new ApplicationException($"Unable to instantiate type {typeof(ILogger)}");
            var pathBuilderRepository = serviceProvider.GetService<IPathBuilderRepository>() ?? throw new ApplicationException($"Unable to instantiate type {typeof(PathBuilderRepository)}");

            using var logScope = logger.BeginScope("RenameFileBlock");

            try
            {
                string filename = Path.GetFileName(parameters.LocalFile!.Path);
                string extension = Path.GetExtension(filename);
                if (extension == null)
                {
                    throw new ApplicationException($"Video file {parameters.LocalFile.Path} has no extension");
                }
                var pathBuilder = pathBuilderRepository.GetPathBuilderForPath(parameters.FileInfo.FullName);

                // Trailing dot is there to prevent Path.ChangeExtension from screwing with the path if it has been ellipsized or has ellipsis in it
                var destinationPathWithoutExtension = pathBuilder.BuildPath(parameters.AnimeFileInfo!, parameters.AnimeInfo!,
                    PlatformUtils.MaxPathLength - extension.Length, parameters.VideoResolution!);

                var destinationPath = destinationPathWithoutExtension + extension;
                var destinationDirectory = Path.GetDirectoryName(destinationPathWithoutExtension);

                if (!configProvider.Config.Debug && !Directory.Exists(destinationDirectory))
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
                            parameters.LocalFile.Status = ImportStatus.Error;
                            parameters.LocalFile.UpdatedAt = DateTimeOffset.Now;
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
                        parameters.LocalFile.Status = parameters.AnimeFileInfo!.HasResolution ? ImportStatus.Imported : ImportStatus.ImportedMissingData;
                        parameters.LocalFile.UpdatedAt = DateTimeOffset.Now;
                        await actionRepository.AddAsync(new FileAction { Type = FileActionType.Copied, Success = true, Info = $"File already exists at {destinationPath}", FileId = parameters.LocalFile.Id });
                        await actionRepository.SaveChangesAsync();
                    }
                    finally
                    {
                        AniSortContext.DatabaseLock.Release();
                    }
                    if (!await localFileRepository.ExistsForPathAsync(parameters.LocalFile.Path))
                    {
                        await localFileRepository.AddAsync(new LocalFile
                        {
                            Path = parameters.LocalFile.Path,
                            Status = parameters.LocalFile.Status,
                            Ed2kHash = parameters.LocalFile.Ed2kHash,
                            EpisodeFileId = parameters.LocalFile.EpisodeFileId,
                            FileLength = parameters.LocalFile.FileLength,
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
                else if (configProvider.Config.Copy)
                {
                    if (!configProvider.Config.Debug)
                    {
                        try
                        {
                            if (configProvider.Config.Verbose)
                            {
                                logger.LogTrace("Destination Path: {DestinationPath}", destinationPath);
                            }

                            File.Copy(parameters.FileInfo.FullName, destinationPath);
                        }
                        catch (UnauthorizedAccessException ex)
                        {
                            logger.LogError("You do not have access to the destination path. Please ensure your user account has access to the destination folder");

                            await AniSortContext.DatabaseLock.WaitAsync();
                            try
                            {
                                parameters.LocalFile.Status = ImportStatus.Error;
                                parameters.LocalFile.UpdatedAt = DateTimeOffset.Now;
                                await localFileRepository.SaveChangesAsync();
                                await actionRepository.AddAsync(new FileAction { Type = configProvider.Config.Copy ? FileActionType.Copy : FileActionType.Move, Success = false, Exception = $"{ex.Message}\n{ex.StackTrace}", FileId = parameters.LocalFile.Id });
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
                                parameters.LocalFile.Status = ImportStatus.Error;
                                parameters.LocalFile.UpdatedAt = DateTimeOffset.Now;
                                await localFileRepository.SaveChangesAsync();
                                await actionRepository.AddAsync(new FileAction { Type = configProvider.Config.Copy ? FileActionType.Copy : FileActionType.Move, Success = false, Exception = $"{ex.Message}\n{ex.StackTrace}", FileId = parameters.LocalFile.Id });
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
                                parameters.LocalFile.Status = ImportStatus.Error;
                                parameters.LocalFile.UpdatedAt = DateTimeOffset.Now;
                                await localFileRepository.SaveChangesAsync();
                                await actionRepository.AddAsync(new FileAction { Type = configProvider.Config.Copy ? FileActionType.Copy : FileActionType.Move, Success = false, Exception = $"{ex.Message}\n{ex.StackTrace}", FileId = parameters.LocalFile.Id });
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
                            parameters.LocalFile.Status = parameters.AnimeFileInfo!.HasResolution ? ImportStatus.Imported : ImportStatus.ImportedMissingData;
                            await localFileRepository.AddAsync(new LocalFile
                            {
                                Path = parameters.LocalFile.Path,
                                Status = parameters.LocalFile.Status,
                                Ed2kHash = parameters.LocalFile.Ed2kHash,
                                EpisodeFileId = parameters.LocalFile.EpisodeFileId,
                                FileLength = parameters.LocalFile.FileLength,
                                FileActions = new List<FileAction> { new() { Type = FileActionType.Copied, Success = true, Info = $"Source file copied to {destinationPath}" } }
                            });
                            parameters.LocalFile.Path = destinationPath;
                            parameters.LocalFile.UpdatedAt = DateTimeOffset.Now;
                            await localFileRepository.SaveChangesAsync();
                            await actionRepository.AddAsync(new FileAction { Type = FileActionType.Copy, Success = true, Info = $"File {parameters.LocalFile.Path} copied to {destinationPath}", FileId = parameters.LocalFile.Id });
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
                    if (!configProvider.Config.Debug)
                    {
                        try
                        {
                            File.Move(parameters.FileInfo.FullName, destinationPath);
                        }
                        catch (UnauthorizedAccessException ex)
                        {
                            logger.LogError(ex,
                                "You do not have access to the destination path. Please ensure your user account has access to the destination folder");

                            await AniSortContext.DatabaseLock.WaitAsync();
                            try
                            {
                                parameters.LocalFile.Status = ImportStatus.Error;
                                parameters.LocalFile.UpdatedAt = DateTimeOffset.Now;
                                await localFileRepository.SaveChangesAsync();
                                await actionRepository.AddAsync(new FileAction { Type = configProvider.Config.Copy ? FileActionType.Copy : FileActionType.Move, Success = false, Exception = $"{ex.Message}\n{ex.StackTrace}", FileId = parameters.LocalFile.Id });
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
                                parameters.LocalFile.Status = ImportStatus.Error;
                                parameters.LocalFile.UpdatedAt = DateTimeOffset.Now;
                                await localFileRepository.SaveChangesAsync();
                                await actionRepository.AddAsync(new FileAction { Type = configProvider.Config.Copy ? FileActionType.Copy : FileActionType.Move, Success = false, Exception = $"{ex.Message}\n{ex.StackTrace}", FileId = parameters.LocalFile.Id });
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
                                parameters.LocalFile.Status = ImportStatus.Error;
                                parameters.LocalFile.UpdatedAt = DateTimeOffset.Now;
                                await localFileRepository.SaveChangesAsync();
                                await actionRepository.AddAsync(new FileAction { Type = configProvider.Config.Copy ? FileActionType.Copy : FileActionType.Move, Success = false, Exception = $"{ex.Message}\n{ex.StackTrace}", FileId = parameters.LocalFile.Id });
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
                            parameters.LocalFile.Status = ImportStatus.Imported;
                            parameters.LocalFile.UpdatedAt = DateTimeOffset.Now;
                            parameters.LocalFile.Path = destinationPath;
                            await localFileRepository.SaveChangesAsync();
                            await actionRepository.AddAsync(new FileAction { Type = FileActionType.Move, Success = true, Info = $"File {parameters.FileInfo.FullName} moved to {destinationPath}", FileId = parameters.LocalFile!.Id });
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

    public ITargetBlock<Job> BuildJobTransformBlock(ITargetBlock<MetadataFileJobParams> fileOutput)
    {
        return new ActionBlock<Job>(async job =>
        {
            var jobStepRepository = serviceProvider.GetService<IJobStepRepository>() ?? throw new ApplicationException($"Cannot instantiate type {typeof(IJobStepRepository)}");
            var jobUpdateProvider = serviceProvider.GetService<IJobUpdateProvider>() ?? throw new ApplicationException($"Cannot instantiate type {typeof(IJobUpdateProvider)}");
            
            var fileQueue = new Queue<string>();
            
            var discoverStep = job.Steps.FirstOrDefault(s => s.Type == StepType.DiscoverFiles);
            
            fileQueue.AddPathsToQueue(job.Options.Fields["path"].StringValue);

            int totalFiles = fileQueue.Count;

            if (discoverStep != null)
            {
                discoverStep.TotalProgress = totalFiles;
                discoverStep.Status = JobStatus.Running;
                await jobStepRepository.UpsertAndDetachAsync(discoverStep);
            }

            int processed = 0;
            int lastPercentNotifiedAt = 0;

            while (fileQueue.TryDequeue(out string? path))
            {
                await fileOutput.SendAsync(new(new System.IO.FileInfo(path), Job: job));
                processed++;
                if (discoverStep != null)
                {
                    discoverStep.CurrentProgress = processed;
                    await jobStepRepository.UpsertAndDetachAsync(discoverStep);
                }

                int percent = processed / totalFiles;
                if (percent > lastPercentNotifiedAt)
                {
                    await jobUpdateProvider.UpdateJobStatusAsync(job);
                    
                    lastPercentNotifiedAt = percent;
                }
            }
            
            
            if (discoverStep != null)
            {
                discoverStep.CurrentProgress = discoverStep.TotalProgress;
                discoverStep.Status = JobStatus.Completed;
                await jobStepRepository.UpsertAndDetachAsync(discoverStep);
            }
        });
    }

    public record MetadataFileJobParams(System.IO.FileInfo FileInfo, LocalFile? LocalFile = default, bool IsCoolingDown = false, FileAnimeInfo? AnimeInfo = default, FileInfo? AnimeFileInfo = default, VideoResolution? VideoResolution = null, Job? Job = null,
        bool Failed = false);
}
