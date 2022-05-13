using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using AniDbSharp.Data;
using AniSort.Core.Data;
using AniSort.Core.Data.Repositories;
using AniSort.Core.IO;
using AniSort.Core.Models;
using AniSort.Core.Utils;
using FFMpegCore;
using Microsoft.Extensions.Logging;

namespace AniSort.Core.MaintenanceTasks;

public class FixUnknownResolutionMaintenanceTask : IMaintenanceTask
{
    private readonly ILocalFileRepository localFileRepository;
    private readonly ILogger<FixUnknownResolutionMaintenanceTask> logger;
    private readonly Config config;

    public FixUnknownResolutionMaintenanceTask(ILocalFileRepository localFileRepository, ILogger<FixUnknownResolutionMaintenanceTask> logger, Config config)
    {
        this.localFileRepository = localFileRepository;
        this.logger = logger;
        this.config = config;
    }

    /// <inheritdoc />
    public async Task RunAsync()
    {
        var filesWithoutResolution = localFileRepository.GetWithoutResolutionAsync();
        
        var pathBuilder = PathBuilder.Compile(
            config.Destination.NewFilePath,
            config.Destination.TvPath,
            config.Destination.MoviePath,
            config.Destination.Format,
            new FileMask { FirstByteFlags = FileMaskFirstByte.AnimeId | FileMaskFirstByte.GroupId | FileMaskFirstByte.EpisodeId, SecondByteFlags = FileMaskSecondByte.Ed2k });

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
    }

    /// <inheritdoc />
    public string UserFacingName => "Fix Files with Unknown Resolution";
}
