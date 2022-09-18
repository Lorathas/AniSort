using AniSort.Core.Data;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace AniSort.Server.Extensions;

public static class ModelExtensions
{
    public static LocalFileReply ToReply(this LocalFile file, bool includeEpisodeFile = true,
        bool includeActions = true)
    {
        var message = new LocalFileReply
        {
            LocalFileId = file.Id.ToString(),
            Path = file.Path,
            FileLength = file.FileLength,
            Status = (ImportStatus)file.Status,
            Ed2KHash = ByteString.CopyFrom(file.Ed2kHash),
            CreatedAt = Timestamp.FromDateTimeOffset(file.CreatedAt),
            UpdatedAt = Timestamp.FromDateTimeOffset(file.UpdatedAt),
        };

        if (file.EpisodeFileId.HasValue)
        {
            message.EpisodeFileId = file.EpisodeFileId.Value;
        }

        if (includeEpisodeFile && file.EpisodeFile != null)
        {
            message.EpisodeFile = file.EpisodeFile.ToReply();
        }

        if (includeActions && file.FileActions.Any())
        {
            message.FileActions.AddRange(file.FileActions.Select(a => a.ToReply(false)));
        }

        return message;
    }

    public static LocalFileUpdateReply ToUpdateReply(this LocalFile file, HubUpdate update, bool includeEpisodeFile = true, bool includeActions = true)
    {
        var message = new LocalFileUpdateReply
        {
            UpdateType = update,
            LocalFileId = file.Id.ToString(),
            Path = file.Path,
            FileLength = file.FileLength,
            Status = (ImportStatus)file.Status,
            Ed2KHash = ByteString.CopyFrom(file.Ed2kHash),
            CreatedAt = Timestamp.FromDateTimeOffset(file.CreatedAt),
            UpdatedAt = Timestamp.FromDateTimeOffset(file.UpdatedAt),
        };

        if (file.EpisodeFileId.HasValue)
        {
            message.EpisodeFileId = file.EpisodeFileId.Value;
        }

        if (includeEpisodeFile && file.EpisodeFile != null)
        {
            message.EpisodeFile = file.EpisodeFile.ToReply();
        }

        if (includeActions && file.FileActions.Any())
        {
            message.FileActions.AddRange(file.FileActions.Select(a => a.ToReply(false)));
        }

        return message;
    }

    public static FileActionReply ToReply(this FileAction action, bool includeLocalFile = true)
    {
        var reply = new FileActionReply
        {
            FileActionId = action.Id.ToString(),
            Type = (FileActionType)action.Type,
            Success = action.Success,
            Info = action.Info,
            Exception = action.Exception,
            CreatedAt = Timestamp.FromDateTimeOffset(action.CreatedAt),
            UpdatedAt = Timestamp.FromDateTimeOffset(action.UpdatedAt),
        };

        if (includeLocalFile && action.File != null)
        {
            reply.LocalFile = action.File.ToReply(true, false);
        }

        return reply;
    }

    public static EpisodeFileReply ToReply(this EpisodeFile file)
    {
        var reply = new EpisodeFileReply
        {
        };

        return reply;
    }

    public static JobLog ToReply(this AniSort.Core.Data.JobLog log) => new()
    {
        JobLogId = log.Id.ToString(),
        JobId = log.JobId.ToString(),
        Message = log.Message,
        Params = log.Params,
        CreatedAt = log.CreatedAt.ToTimestamp(),
    };

    public static JobReply ToReply(this Job job)
    {
        var reply = new JobReply
        {
            JobId = job.Id.ToString(),
            Name = job.Name,
            Type = (JobType)job.Type,
            Status = (JobStatus)job.Status,
            PercentComplete = job.PercentComplete,
            IsFinished = job.IsFinished,
            StartedAt = job.StartedAt?.ToTimestamp(),
            CompletedAt = job.CompletedAt?.ToTimestamp()
        };
        reply.Steps.AddRange(job.Steps.Select(s => s.ToReply()));

        return reply;
    }

    public static JobStep ToReply(this AniSort.Core.Data.JobStep step) => new()
    {
        StepId = step.Id.ToString(),
        Name = step.Name,
        Status = (JobStatus)step.Status,
        PercentComplete = step.PercentComplete,
        StartedAt = step.StartedAt?.ToTimestamp(),
        CompletedAt = step.CompletedAt?.ToTimestamp(),
    };

    public static JobDetailsReply.Types.JobStepDetails ToDetailsReply(this AniSort.Core.Data.JobStep step)
    {
        var stepDetails = new JobDetailsReply.Types.JobStepDetails
        {
            Name = step.Name, StartedAt = step.StartedAt?.ToTimestamp(), CompletedAt = step.CompletedAt?.ToTimestamp()
        };

        stepDetails.Logs.AddRange(step.Logs.Select(l => new StepLog
        {
            StepLogId = l.Id.ToString(), Message = l.Message, Params = l.Params, CreatedAt = l.CreatedAt.ToTimestamp()
        }));

        return stepDetails;
    }

    public static JobDetailsReply ToDetailsReply(this Job job)
    {
        var reply = new JobDetailsReply();

        reply.Logs.AddRange(job.Logs.Select(l => l.ToReply()));

        reply.Steps.AddRange(job.Steps.Select(s => s.ToDetailsReply()));

        return reply;
    }

    public static Job ToJob(this QueueJobRequest request)
    {
        return new Job
        {
            Name = request.Name,
            Options = request.Options,
            Type = (Core.Data.JobType) request.Type,
            QueuedAt = DateTimeOffset.Now
        };
    }

    public static ScheduledJob ToReply(this Core.Data.ScheduledJob scheduledJob)
    {
        return new ScheduledJob
        {
            ScheduledJobId = scheduledJob.Id.ToString(),
            Name = scheduledJob.Name,
            Type = (JobType) scheduledJob.Type,
            ScheduleType = (ScheduleType) scheduledJob.ScheduleType,
            Options = scheduledJob.Options,
            ScheduleOptions = scheduledJob.ScheduleOptions,
        };
    }

    public static ScheduledJobUpdate ToReply(this Core.Data.ScheduledJob scheduledJob, HubUpdate update)
    {
        return new ScheduledJobUpdate
        {
            ScheduledJobId = scheduledJob.Id.ToString(),
            Name = scheduledJob.Name,
            Type = (JobType) scheduledJob.Type,
            ScheduleType = (ScheduleType) scheduledJob.ScheduleType,
            Options = scheduledJob.Options,
            ScheduleOptions = scheduledJob.ScheduleOptions,
            Update = update
        };
    }

    public static string ToJobNamePart(this Core.Data.ScheduleType scheduleType)
    {
        switch (scheduleType)
        {
            case Core.Data.ScheduleType.Timed:
                return "timer";
            case Core.Data.ScheduleType.OnFileChange:
                return "file change";
            default:
                throw new ArgumentOutOfRangeException(nameof(scheduleType), scheduleType, null);
        }
    }
}