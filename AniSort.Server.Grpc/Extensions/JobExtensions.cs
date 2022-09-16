using AniSort.Core.Data;
using Google.Protobuf.WellKnownTypes;

namespace AniSort.Server.Extensions;

public static class JobExtensions
{

    #region Job Step Names

    // ReSharper disable once MemberCanBePrivate.Global
    public const string HashStepStep = "Hash";

    // ReSharper disable once MemberCanBePrivate.Global
    public const string FetchFileMetadataStep = "Fetch File Metadata";

    // ReSharper disable once MemberCanBePrivate.Global
    public const string MoveFileStep = "Move File";

    // ReSharper disable once MemberCanBePrivate.Global
    public const string LookupExistingMetadataStep = "Lookup Existing Metadata";

    // ReSharper disable once MemberCanBePrivate.Global
    public const string DiscoverFragmentedSeriesStep = "Discover Fragmented Series";

    // ReSharper disable once MemberCanBePrivate.Global
    public const string DetermineDefragmentFolderStep = "Determine Defragment Folder";

    // ReSharper disable once MemberCanBePrivate.Global
    public const string DefragmentSeriesStep = "Defragment Series Step";

    #endregion

    #region Job Metadata Keys

    public const string FilePathKey = "filePath";

    public const string DirectoryPathKey = "directoryPath";

    #endregion

    public static Job ToJob(this Core.Data.ScheduledJob scheduledJob, string? path = null)
    {
        switch (scheduledJob.Type)
        {
            case Core.Data.JobType.SortFile:
                return scheduledJob.ToSortFileJob(path);
            case Core.Data.JobType.SortDirectory:
                return string.IsNullOrWhiteSpace(path)
                    ? scheduledJob.ToSortDirectoryJob()
                    : scheduledJob.ToSortFileJob(path);
            case Core.Data.JobType.HashFile:
                return scheduledJob.ToHashFileJob(path);
            case Core.Data.JobType.HashDirectory:
                return path == null
                    ? scheduledJob.ToHashDirectoryJob()
                    : scheduledJob.ToHashFileJob(path);
            case Core.Data.JobType.Defragment:
                return scheduledJob.ToDefragmentJob();
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    public static Job ToSortFileJob(this Core.Data.ScheduledJob scheduledJob, string? path = null)
    {
        var options = scheduledJob.Options.Clone();

        if (!string.IsNullOrWhiteSpace(path))
        {
            options.Fields[FilePathKey].StringValue = path;
        }
        
        return new Job
        {
            ScheduledJobId = scheduledJob.Id,
            Name = $"Scheduled {scheduledJob.Type} from {scheduledJob.ScheduleType}",
            Type = scheduledJob.Type,
            QueuedAt = DateTimeOffset.Now,
            Status = Core.Data.JobStatus.Queued,
            Options = options,
            Steps = new List<Core.Data.JobStep>
            {
                new()
                {
                    Name = HashStepStep,
                    Status = Core.Data.JobStatus.Queued,
                    PercentComplete = 0
                },
                new()
                {
                    Name = LookupExistingMetadataStep,
                    Status = Core.Data.JobStatus.Queued,
                    PercentComplete = 0
                },
                new()
                {
                    Name = FetchFileMetadataStep,
                    Status = Core.Data.JobStatus.Queued,
                    PercentComplete = 0
                },
                new()
                {
                    Name = MoveFileStep,
                    Status = Core.Data.JobStatus.Queued,
                    PercentComplete = 0
                }
            }
        };
    }

    public static Job ToSortDirectoryJob(this Core.Data.ScheduledJob scheduledJob)
    {
        return new Job
        {
            ScheduledJobId = scheduledJob.Id,
            Name = $"Scheduled {scheduledJob.Type} from {scheduledJob.ScheduleType}",
            Type = scheduledJob.Type,
            QueuedAt = DateTimeOffset.Now,
            Status = Core.Data.JobStatus.Queued,
            Options = scheduledJob.Options,
            Steps = new List<Core.Data.JobStep>
            {
                new()
                {
                    Name = HashStepStep,
                    Status = Core.Data.JobStatus.Queued,
                    PercentComplete = 0
                },
                new()
                {
                    Name = LookupExistingMetadataStep,
                    Status = Core.Data.JobStatus.Queued,
                    PercentComplete = 0
                },
                new()
                {
                    Name = FetchFileMetadataStep,
                    Status = Core.Data.JobStatus.Queued,
                    PercentComplete = 0
                },
                new()
                {
                    Name = MoveFileStep,
                    Status = Core.Data.JobStatus.Queued,
                    PercentComplete = 0
                }
            }
        };
    }

    public static Job ToHashFileJob(this Core.Data.ScheduledJob scheduledJob, string? path = null)
    {
        var options = scheduledJob.Options.Clone();

        if (!string.IsNullOrWhiteSpace(path))
        {
            options.Fields[FilePathKey].StringValue = path;
        }
        
        return new Job
        {
            ScheduledJobId = scheduledJob.Id,
            Name = $"Scheduled {scheduledJob.Type} from {scheduledJob.ScheduleType}",
            Type = scheduledJob.Type,
            QueuedAt = DateTimeOffset.Now,
            Status = Core.Data.JobStatus.Queued,
            Options = options,
            Steps = new List<Core.Data.JobStep>
            {
                new()
                {
                    Name = HashStepStep,
                    Status = Core.Data.JobStatus.Queued,
                    PercentComplete = 0
                },
                new()
                {
                    Name = LookupExistingMetadataStep,
                    Status = Core.Data.JobStatus.Queued,
                    PercentComplete = 0
                }
            }
        };
    }

    public static Job ToHashDirectoryJob(this Core.Data.ScheduledJob scheduledJob)
    {
        return new Job
        {
            ScheduledJobId = scheduledJob.Id,
            Name = $"Scheduled {scheduledJob.Type} from {scheduledJob.ScheduleType}",
            Type = scheduledJob.Type,
            QueuedAt = DateTimeOffset.Now,
            Status = Core.Data.JobStatus.Queued,
            Options = scheduledJob.Options,
            Steps = new List<Core.Data.JobStep>
            {
                new()
                {
                    Name = HashStepStep,
                    Status = Core.Data.JobStatus.Queued,
                    PercentComplete = 0
                },
                new()
                {
                    Name = LookupExistingMetadataStep,
                    Status = Core.Data.JobStatus.Queued,
                    PercentComplete = 0
                }
            }
        };
    }

    public static Job ToDefragmentJob(this Core.Data.ScheduledJob scheduledJob)
    {
        return new Job
        {
            ScheduledJobId = scheduledJob.Id,
            Name = $"Scheduled {scheduledJob.Type} from {scheduledJob.ScheduleType}",
            Type = scheduledJob.Type,
            QueuedAt = DateTimeOffset.Now,
            Status = Core.Data.JobStatus.Queued,
            Options = scheduledJob.Options,
            Steps = new List<Core.Data.JobStep>
            {
                new()
                {
                    Name = DiscoverFragmentedSeriesStep,
                    Status = Core.Data.JobStatus.Queued,
                    PercentComplete = 0
                },
                new()
                {
                    Name = DetermineDefragmentFolderStep,
                    Status = Core.Data.JobStatus.Queued,
                    PercentComplete = 0
                },
                new()
                {
                    Name = DefragmentSeriesStep,
                    Status = Core.Data.JobStatus.Queued,
                    PercentComplete = 0
                }
            }
        };
    }
}
