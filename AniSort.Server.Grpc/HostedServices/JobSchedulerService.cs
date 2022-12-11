using System.Diagnostics;
using System.Threading.Channels;
using AniSort.Core.Data;
using AniSort.Core.Data.Repositories;
using AniSort.Core.Data.Repositories.EF;
using AniSort.Core.Extensions;
using AniSort.Server.DataStructures;
using AniSort.Server.Extensions;
using AniSort.Server.Hubs;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace AniSort.Server.HostedServices;

public class JobSchedulerService : BackgroundService
{
    private readonly ILogger<JobSchedulerService> logger;

    private readonly ActivitySource activitySource;

    private readonly IScheduledJobHub scheduledJobHub;

    private readonly IJobHub jobHub;

    private readonly IServiceProvider serviceProvider;

    private readonly Channel<Job> pendingJobChannel = Channel.CreateUnbounded<Job>();

    private readonly ConcurrentSet<TimedJobInfo> timerJobs = new();

    private readonly ConcurrentSet<FileWatcherJobInfo> watcherJobs = new();

    /// <inheritdoc />
    public JobSchedulerService(ILogger<JobSchedulerService> logger, ActivitySource activitySource, IScheduledJobHub scheduledJobHub, IJobHub jobHub, IServiceProvider serviceProvider)
    {
        this.logger = logger;
        this.activitySource = activitySource;
        this.scheduledJobHub = scheduledJobHub;
        this.jobHub = jobHub;
        this.serviceProvider = serviceProvider;
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        List<Core.Data.ScheduledJob> scheduledJobs;
        await using (var scope = serviceProvider.CreateAsyncScope())
        await using (var scheduledJobRepository = scope.ServiceProvider.GetService<IScheduledJobRepository>())
        {
            scheduledJobs = await scheduledJobRepository!.GetForQueue().ToListAsync(cancellationToken: stoppingToken);
        }

        if (stoppingToken.IsCancellationRequested)
        {
            return;
        }

        foreach (var scheduledJob in scheduledJobs)
        {
            await QueueJobAsync(scheduledJob, stoppingToken);
        }
        
        await scheduledJobHub.RegisterListenerAsync(async (scheduledJob, update) =>
        {
            switch (update)
            {
                case HubUpdate.ItemCreated:
                    await QueueJobAsync(scheduledJob, stoppingToken);
                    break;
                case HubUpdate.ItemUpdated:
                    await UpdateScheduledJobAsync(scheduledJob, stoppingToken);
                    break;
                case HubUpdate.ItemDeleted:
                    await RemoveScheduledJobAsync(scheduledJob);
                    break;
            }
        }, stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            var job = await pendingJobChannel.Reader.ReadAsync(stoppingToken);

            if (stoppingToken.IsCancellationRequested)
            {
                break;
            }

            await using var jobRepository = serviceProvider.GetService<IJobRepository>();

            job = await jobRepository!.AddAsync(job);
            await jobRepository.SaveChangesAsync();
            jobRepository.Detach(job);

            await jobHub.PublishUpdateAsync(job, JobUpdate.JobCreated);
        }

        await Task.WhenAll(timerJobs.Select(j => j.TimerTask));

        foreach (var fileWatcherJobInfo in watcherJobs)
        {
            fileWatcherJobInfo.Watchers.ForEach(w => w.Dispose());
        }
    }

    private async Task QueueJobAsync(Core.Data.ScheduledJob job, CancellationToken stoppingToken)
    {
        switch (job.ScheduleType)
        {
            case Core.Data.ScheduleType.Timed:
                timerJobs.TryAdd(QueueTimedJob(job, stoppingToken));
                break;
            case Core.Data.ScheduleType.OnFileChange:
                watcherJobs.TryAdd(await QueueWatcherJobAsync(job, stoppingToken));
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private TimedJobInfo QueueTimedJob(Core.Data.ScheduledJob scheduledJob, CancellationToken cancellationToken)
    {
        var cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var childCancellationToken = cancellationTokenSource.Token;

        var timerTask = Task.Run(async () =>
        {
            while (!childCancellationToken.IsCancellationRequested)
            {
                await using var jobRepository = serviceProvider.GetService<JobRepository>();

                var lastRun = await jobRepository!.GetLastJobForScheduledJobAsync(scheduledJob.Id, cancellationToken);

                var jobTime = TimeSpan.Parse(scheduledJob.ScheduleOptions.Fields["interval"].StringValue);

                var diff = DateTimeOffset.Now.Subtract(lastRun?.CompletedAt ?? DateTimeOffset.UnixEpoch);

                if (lastRun?.Status == Core.Data.JobStatus.Completed && diff < jobTime)
                {
                    await Task.Delay(jobTime - diff, childCancellationToken);
                }

                await pendingJobChannel.Writer.WriteAsync(scheduledJob.ToJob(), childCancellationToken);
            }
        }, childCancellationToken);

        return new TimedJobInfo(scheduledJob, timerTask, cancellationTokenSource);
    }

    private async Task<FileWatcherJobInfo> QueueWatcherJobAsync(Core.Data.ScheduledJob scheduledJob, CancellationToken cancellationToken)
    {
        void Changed(object sender, FileSystemEventArgs args)
        {
            pendingJobChannel.Writer.WriteAsync(scheduledJob.ToJob(args.FullPath), cancellationToken).GetAwaiter().GetResult();
        }

        void Created(object sender, FileSystemEventArgs args)
        {
            pendingJobChannel.Writer.WriteAsync(scheduledJob.ToJob(args.FullPath), cancellationToken).GetAwaiter().GetResult();
        }

        void Deleted(object sender, FileSystemEventArgs args)
        {
            pendingJobChannel.Writer.WriteAsync(scheduledJob.ToJob(args.FullPath), cancellationToken).GetAwaiter().GetResult();
        }

        void Renamed(object sender, RenamedEventArgs args)
        {
            pendingJobChannel.Writer.WriteAsync(scheduledJob.ToJob(args.FullPath), cancellationToken).GetAwaiter().GetResult();
        }

        switch (scheduledJob.Type)
        {
            case Core.Data.JobType.SortFile:
            case Core.Data.JobType.HashFile:
                return await QueueFileWatcherJob(scheduledJob, Created, Changed, Deleted, Renamed, cancellationToken);
            case Core.Data.JobType.SortDirectory:
            case Core.Data.JobType.HashDirectory:
                return await QueueDirectoryWatcherJob(scheduledJob, Created, Changed, Deleted, Renamed, cancellationToken);
            case Core.Data.JobType.Defragment:
                throw new UnsupportedContentTypeException("Defragment jobs cannot be scheduled with a file watcher");
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private async Task<FileWatcherJobInfo> QueueFileWatcherJob(Core.Data.ScheduledJob scheduledJob, FileSystemEventHandler created, FileSystemEventHandler changed, FileSystemEventHandler deleted, RenamedEventHandler renamed, CancellationToken cancellationToken)
    {
        string path = scheduledJob.Options.Fields[JobExtensions.FilePathKey].StringValue;

        var watcher = CreateFileSystemWatcher(path);

        watcher.Created += created;
        watcher.Changed += changed;
        watcher.Deleted += deleted;
        watcher.Renamed += renamed;

        await pendingJobChannel.Writer.WriteAsync(scheduledJob.ToJob(), cancellationToken);

        return new FileWatcherJobInfo(scheduledJob, new List<FileSystemWatcher>
        {
            watcher
        });
    }

    private async Task<FileWatcherJobInfo> QueueDirectoryWatcherJob(Core.Data.ScheduledJob scheduledJob, FileSystemEventHandler created, FileSystemEventHandler changed, FileSystemEventHandler deleted, RenamedEventHandler renamed, CancellationToken cancellationToken)
    {
        string path = scheduledJob.Options.Fields[JobExtensions.DirectoryPathKey].StringValue;

        var watchers = FileImportExtensions.SupportedFileExtensions.Select(e =>
        {
            var watcher = CreateFileSystemWatcher(path);
            watcher.Filter = $"*.{e}";
            watcher.Created += created;
            watcher.Changed += changed;
            watcher.Deleted += deleted;
            watcher.Renamed += renamed;

            return watcher;
        }).ToList();

        await pendingJobChannel.Writer.WriteAsync(scheduledJob.ToJob(), cancellationToken);

        return new FileWatcherJobInfo(scheduledJob, watchers);
    }

    private async Task UpdateScheduledJobAsync(Core.Data.ScheduledJob scheduledJob, CancellationToken cancellationToken)
    {
        switch (scheduledJob.ScheduleType)
        {
            case Core.Data.ScheduleType.Timed:
                await UpdateTimerScheduledJobAsync(scheduledJob, cancellationToken);
                break;
            case Core.Data.ScheduleType.OnFileChange:
                await UpdateWatcherScheduledJobAsync(scheduledJob, cancellationToken);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private async Task UpdateTimerScheduledJobAsync(Core.Data.ScheduledJob scheduledJob, CancellationToken cancellationToken)
    {
        var existing = timerJobs.FirstOrDefault(j => j.ScheduledJob.Id == scheduledJob.Id);

        if (existing != default)
        {
            timerJobs.TryRemove(existing);
            existing.CancellationTokenSource.Cancel();
            await existing.TimerTask;
        }

        QueueTimedJob(scheduledJob, cancellationToken);
    }

    private async Task UpdateWatcherScheduledJobAsync(Core.Data.ScheduledJob scheduledJob, CancellationToken cancellationToken)
    {
        var existing = watcherJobs.FirstOrDefault(j => j.ScheduledJob.Id == scheduledJob.Id);

        if (existing != default)
        {
            watcherJobs.TryRemove(existing);
            existing.Watchers.ForEach(w => w.Dispose());
        }

        await QueueWatcherJobAsync(scheduledJob, cancellationToken);
    }

    private async Task RemoveScheduledJobAsync(Core.Data.ScheduledJob scheduledJob)
    {
        switch (scheduledJob.ScheduleType)
        {
            case Core.Data.ScheduleType.Timed:
                await RemoveTimerScheduledJobAsync(scheduledJob);
                break;
            case Core.Data.ScheduleType.OnFileChange:
                RemoveWatcherScheduledJob(scheduledJob);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private async Task RemoveTimerScheduledJobAsync(Core.Data.ScheduledJob scheduledJob)
    {
        var existing = timerJobs.FirstOrDefault(j => j.ScheduledJob.Id == scheduledJob.Id);

        if (existing != default)
        {
            timerJobs.TryRemove(existing);
            existing.CancellationTokenSource.Cancel();
            await existing.TimerTask;
        }
    }

    private void RemoveWatcherScheduledJob(Core.Data.ScheduledJob scheduledJob)
    {
        var existing = watcherJobs.FirstOrDefault(j => j.ScheduledJob.Id == scheduledJob.Id);

        if (existing == default) return;

        watcherJobs.TryRemove(existing);
        existing.Watchers.ForEach(w => w.Dispose());
    }

    private static FileSystemWatcher CreateFileSystemWatcher(string path) => new(path)
    {
        NotifyFilter = NotifyFilters.Size | NotifyFilters.LastWrite | NotifyFilters.CreationTime | NotifyFilters.DirectoryName | NotifyFilters.FileName,
        IncludeSubdirectories = true,
        EnableRaisingEvents = true
    };

    private record TimedJobInfo(Core.Data.ScheduledJob ScheduledJob, Task TimerTask, CancellationTokenSource CancellationTokenSource)
    {
        /// <inheritdoc />
        public override int GetHashCode() => ScheduledJob.Id.GetHashCode();

        /// <inheritdoc />
        public virtual bool Equals(TimedJobInfo? other) => ScheduledJob.Id == other?.ScheduledJob.Id;
    }

    private record FileWatcherJobInfo(Core.Data.ScheduledJob ScheduledJob, List<FileSystemWatcher> Watchers)
    {
        /// <inheritdoc />
        public virtual bool Equals(FileWatcherJobInfo? other) => ScheduledJob.Id == other?.ScheduledJob.Id;

        /// <inheritdoc />
        public override int GetHashCode() => ScheduledJob.Id.GetHashCode();
    }
}
