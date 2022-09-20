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

    public const string DiscoverFilesStep = "Discover Files";

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
            Status = Core.Data.JobStatus.Created,
            Options = options,
            Steps = new List<Core.Data.JobStep>
            {
                new()
                {
                    Name = HashStepStep,
                    Type = StepType.Hash,
                    Status = Core.Data.JobStatus.Created,
                },
                new()
                {
                    Name = LookupExistingMetadataStep,
                    Type = StepType.FetchLocalFile,
                    Status = Core.Data.JobStatus.Created,
                },
                new()
                {
                    Name = FetchFileMetadataStep,
                    Type = StepType.FetchMetadata,
                    Status = Core.Data.JobStatus.Created,
                },
                new()
                {
                    Name = MoveFileStep,
                    Type = StepType.Sort,
                    Status = Core.Data.JobStatus.Created,
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
            Status = Core.Data.JobStatus.Created,
            Options = scheduledJob.Options,
            Steps = new List<Core.Data.JobStep>
            {
                new()
                {
                    Name = DiscoverFilesStep,
                    Type = StepType.DiscoverFiles,
                    Status = Core.Data.JobStatus.Created,
                },
                new()
                {
                    Name = HashStepStep,
                    Type = StepType.Hash,
                    Status = Core.Data.JobStatus.Created,
                },
                new()
                {
                    Name = LookupExistingMetadataStep,
                    Type = StepType.FetchLocalFile,
                    Status = Core.Data.JobStatus.Created,
                },
                new()
                {
                    Name = FetchFileMetadataStep,
                    Type = StepType.FetchMetadata,
                    Status = Core.Data.JobStatus.Created,
                },
                new()
                {
                    Name = MoveFileStep,
                    Type = StepType.Sort,
                    Status = Core.Data.JobStatus.Created,
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
            Status = Core.Data.JobStatus.Created,
            Options = options,
            Steps = new List<Core.Data.JobStep>
            {
                new()
                {
                    Name = HashStepStep,
                    Type = StepType.Hash,
                    Status = Core.Data.JobStatus.Created,
                },
                new()
                {
                    Name = LookupExistingMetadataStep,
                    Type = StepType.FetchLocalFile,
                    Status = Core.Data.JobStatus.Created,
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
            Status = Core.Data.JobStatus.Created,
            Options = scheduledJob.Options,
            Steps = new List<Core.Data.JobStep>
            {
                new()
                {
                    Name = DiscoverFilesStep,
                    Type = StepType.DiscoverFiles,
                    Status = Core.Data.JobStatus.Created,
                },
                new()
                {
                    Name = HashStepStep,
                    Type = StepType.FetchLocalFile,
                    Status = Core.Data.JobStatus.Created,
                },
                new()
                {
                    Name = LookupExistingMetadataStep,
                    Type = StepType.FetchLocalFile,
                    Status = Core.Data.JobStatus.Created,
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
            Status = Core.Data.JobStatus.Created,
            Options = scheduledJob.Options,
            Steps = new List<Core.Data.JobStep>
            {
                new()
                {
                    Name = DiscoverFragmentedSeriesStep,
                    Type = StepType.DiscoverFragmentedSeries,
                    Status = Core.Data.JobStatus.Created,
                },
                new()
                {
                    Name = DetermineDefragmentFolderStep,
                    Type = StepType.DetermineDefragmentFolder,
                    Status = Core.Data.JobStatus.Created,
                },
                new()
                {
                    Name = DefragmentSeriesStep,
                    Type = StepType.DefragmentSeries,
                    Status = Core.Data.JobStatus.Created,
                }
            }
        };
    }

    public static JobUpdate ToJobUpdate(this Job job)
    {
        switch (job.Status)
        {
            case Core.Data.JobStatus.Created:
                return JobUpdate.JobCreated;
            case Core.Data.JobStatus.Queued:
                return JobUpdate.JobStarted;
            case Core.Data.JobStatus.Running:
                return job.PercentComplete == 0 ? JobUpdate.JobStarted : JobUpdate.JobProgress;
            case Core.Data.JobStatus.Completed:
                return JobUpdate.JobCompleted;
            case Core.Data.JobStatus.Failed:
                return JobUpdate.JobFailed;
            default:
                throw new ArgumentOutOfRangeException(nameof(job.Status), job.Status, null);
        }
    }
}
