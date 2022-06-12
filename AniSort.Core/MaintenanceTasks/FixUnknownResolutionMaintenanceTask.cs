using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using AniDbSharp;
using AniDbSharp.Data;
using AniSort.Core.Data;
using AniSort.Core.Data.Repositories;
using AniSort.Core.Extensions;
using AniSort.Core.IO;
using AniSort.Core.Models;
using AniSort.Core.Utils;
using FFMpegCore;
using Microsoft.Extensions.Logging;

namespace AniSort.Core.MaintenanceTasks;

public class FixUnknownResolutionMaintenanceTask : IMaintenanceTask
{
    private static readonly Regex ResolutionReplacementRegex = new(@"(.*)(\[(0x0)\])(.*)", RegexOptions.Compiled);
    
    private readonly ILocalFileRepository localFileRepository;
    private readonly ILogger<FixUnknownResolutionMaintenanceTask> logger;
    private readonly Config config;
    private readonly IPathBuilderRepository pathBuilderRepository;

    public FixUnknownResolutionMaintenanceTask(ILocalFileRepository localFileRepository, ILogger<FixUnknownResolutionMaintenanceTask> logger, Config config, IPathBuilderRepository pathBuilderRepository)
    {
        this.localFileRepository = localFileRepository;
        this.logger = logger;
        this.config = config;
        this.pathBuilderRepository = pathBuilderRepository;
    }

    /// <inheritdoc />
    public async Task RunAsync()
    {
        var filesWithoutResolution = localFileRepository.GetWithoutResolutionAsync();

        await foreach (var file in filesWithoutResolution)
        {
            if (!File.Exists(file.Path) || file.EpisodeFile == null)
            {
                logger.LogWarning("File {FilePath} no longer exists, removing from database", file.Path);
                if (!config.Debug)
                {
                    await localFileRepository.RemoveAsync(file);
                    await localFileRepository.SaveChangesAsync();
                }
                continue;
            }

            var mediaInfo = await FFProbe.AnalyseAsync(file.Path);

            if (mediaInfo.PrimaryVideoStream == null)
            {
                logger.LogError("No primary video stream for file {FilePath}", file.Path);
                continue;
            }

            var resolution = new VideoResolution(mediaInfo.PrimaryVideoStream.Width, mediaInfo.PrimaryVideoStream.Height);

            file.EpisodeFile.Resolution = resolution;

            var pathBuilder = pathBuilderRepository.GetPathBuilderForPath(file.Path);

            string extension = Path.GetExtension(file.Path);
            Debug.Assert(extension != null, nameof(extension) + " != null");
            var destinationPathWithoutExtension = pathBuilder.BuildPath(file, PlatformUtils.MaxPathLength - extension.Length, resolution);

            var destinationPath = destinationPathWithoutExtension + extension;
            var destinationDirectory = Path.GetDirectoryName(destinationPathWithoutExtension);
            
            if (!Directory.Exists(destinationDirectory) && !config.Debug)
            {
                Debug.Assert(destinationDirectory != null, nameof(destinationDirectory) + " != null");
                Directory.CreateDirectory(destinationDirectory);
            }
            
            if (config.Copy)
            {
                if (!File.Exists(destinationPath))
                {
                    logger.LogInformation("Copying {FilePath} to {DestinationPath}", file.Path, destinationPath);
                    if (!config.Debug)
                    {
                        File.Copy(file.Path, destinationPath);
                        file.FileActions.Add(new FileAction { Type = FileActionType.Copy });
                    }
                    logger.LogInformation("Copied {FilePath} to {DestinationPath}", file.Path, destinationPath);
                }
                else
                {
                    logger.LogWarning("File {FilePath} already exists at {DestinationPath}", file.Path, destinationPath);
                    if (!config.Debug)
                    {
                        file.FileActions.Add(new FileAction { Type = FileActionType.Copied, Success = true, Info = "File already exists, skipping" });
                    }
                }
            }
            else
            {
                if (!File.Exists(destinationPath))
                {
                    logger.LogDebug("Moving {FilePath} to {DestinationPath}", file.Path, destinationPath);
                    if (!config.Debug)
                    {
                        File.Move(file.Path, destinationPath);
                        file.Path = destinationPath;
                    }
                    logger.LogInformation("Moved {FilePath} to {DestinationPath}", file.Path, destinationPath);
                }
                else
                {
                    logger.LogWarning("File {FilePath} already exists at {DestinationPath}", file.Path, destinationPath);
                    if (!config.Debug)
                    {
                        var existing = await localFileRepository.GetForPathAsync(destinationPath);

                        if (existing == default)
                        {
                            await localFileRepository.AddAsync(new LocalFile
                            {
                                Path = file.Path,
                                Status = file.Status,
                                Ed2kHash = file.Ed2kHash,
                                EpisodeFileId = file.EpisodeFileId,
                                FileLength = file.FileLength,
                                FileActions = new List<FileAction> { new() { Type = FileActionType.Copied, Success = true, Info = $"File already exists at {destinationPath}" } }
                            });
                        }
                        else
                        {
                            existing.FileActions.Add(new FileAction { Type = FileActionType.Copied, Success = true, Info = $"File already exists at {destinationPath}" });
                        }
                        
                        file.FileActions.Add(new FileAction { Type = FileActionType.Copied, Success = true, Info = $"File already exists at {destinationPath}" });
                    }
                }
            }

            if (!config.Debug)
            {
                await localFileRepository.SaveChangesAsync();
            }
        }

        try
        {
            var flow = BuildProcessingFlow();

            flow.AddPathsToFlow(config.Sources, p => ResolutionReplacementRegex.IsMatch(p));
            flow.AddPathsToFlow(config.LibraryPaths, p => ResolutionReplacementRegex.IsMatch(p));
            flow.Complete();

            await flow.Completion;
        }
        catch (AggregateException ex)
        {
            ex.Handle(innerEx =>
            {
                logger.LogError(innerEx, "An error occurred while processing files without an resolution in the filename");
                return true;
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while processing files without an resolution in the filename");
        }
    }

    private BufferBlock<string> BuildProcessingFlow()
    {
        var inputBuffer = new BufferBlock<string>();
        var grabInfoBlock = new TransformBlock<string, (string Path, IMediaAnalysis MediaInfo)>(path => (path, FFProbe.Analyse(path)));
        inputBuffer.LinkTo(grabInfoBlock);
        var renameBlock = new ActionBlock<(string Path, IMediaAnalysis MediaInfo)>(async info =>
        {
            var (path, mediaInfo) = info;

            var localFile = await localFileRepository.GetForPathAsync(path);

            if (localFile == default)
            {
                logger.LogDebug("New file {FilePath} found", path);
                if (!config.Debug)
                {
                    localFile = await localFileRepository.AddAsync(new LocalFile { Path = path, Status = ImportStatus.NotYetImported });
                    await localFileRepository.SaveChangesAsync();
                }
            }
            else
            {
                logger.LogDebug("File {FilePath} found with missing resolution", path);
            }

            string newPath = ResolutionReplacementRegex.Replace(path, $"$1[{mediaInfo.PrimaryVideoStream.Width}x{mediaInfo.PrimaryVideoStream.Height}]$4");

            if (!File.Exists(newPath))
            {
                logger.LogInformation("Moving file {OldPath} to {NewPath}", path, newPath);
                if (!config.Debug)
                {
                    File.Move(path, newPath);
                    localFile.Path = newPath;
                    await localFileRepository.SaveChangesAsync();
                }
                logger.LogDebug("Moved file {OldPath} to {NewPath}", path, newPath);
            }
        });
        grabInfoBlock.LinkTo(renameBlock);

        return inputBuffer;
    }

    /// <inheritdoc />
    public string Description => "Fix Files with Unknown Resolution";

    /// <inheritdoc />
    public string CommandName => "resfix";
}
