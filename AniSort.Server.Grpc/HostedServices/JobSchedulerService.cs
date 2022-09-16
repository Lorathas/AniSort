using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading.Channels;
using AniSort.Core.Data;
using AniSort.Core.Data.Repositories;
using AniSort.Core.Extensions;
using AniSort.Server.DataStructures;
using AniSort.Server.Extensions;
using AniSort.Server.Hubs;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Win32;
using ScheduledJob = AniSort.Core.Data.ScheduledJob;

namespace AniSort.Server.HostedServices;

public class JobSchedulerService : BackgroundService
{
    private readonly ILogger<JobSchedulerService> logger;

    private readonly ActivitySource activitySource;

    private readonly IScheduledJobHub scheduledJobHub;

    private readonly IJobHub jobHub;

    private readonly IServiceProvider serviceProvider;

    private readonly Channel<Job> pendingJobChannel = Channel.CreateUnbounded<Job>();

    private readonly List<Task> timerJobs = new();

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
        await using (var scheduledJobRepository = serviceProvider.GetService<IScheduledJobRepository>())
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
            if (update == HubUpdate.ItemCreated)
            {
                await QueueJobAsync(scheduledJob, stoppingToken);
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

        await Task.WhenAll(timerJobs);

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
                timerJobs.Add(QueueTimedJobAsync(job, stoppingToken));
                break;
            case Core.Data.ScheduleType.OnFileChange:
                watcherJobs.TryAdd(await QueueWatcherJobAsync(job, stoppingToken));
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private async Task QueueTimedJobAsync(Core.Data.ScheduledJob scheduledJob, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            await using var jobRepository = serviceProvider.GetService<JobRepository>();

            var lastRun = await jobRepository!.GetLastJobForScheduledJobAsync(scheduledJob.Id, cancellationToken);

            var jobTime = TimeSpan.Parse(scheduledJob.ScheduleOptions.Fields["interval"].StringValue);

            var diff = DateTimeOffset.Now.Subtract(lastRun?.CompletedAt ?? DateTimeOffset.UnixEpoch);

            if (lastRun?.Status == Core.Data.JobStatus.Completed && diff < jobTime)
            {
                await Task.Delay(jobTime - diff, cancellationToken);
            }

            await pendingJobChannel.Writer.WriteAsync(scheduledJob.ToJob(), cancellationToken);
        }
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

    private static FileSystemWatcher CreateFileSystemWatcher(string path) => new(path)
    {
        NotifyFilter = NotifyFilters.Size | NotifyFilters.LastWrite | NotifyFilters.CreationTime | NotifyFilters.DirectoryName | NotifyFilters.FileName,
        IncludeSubdirectories = true,
        EnableRaisingEvents = true
    };

    private record FileWatcherJobInfo(Core.Data.ScheduledJob ScheduledJob, List<FileSystemWatcher> Watchers)
    {
        /// <inheritdoc />
        public virtual bool Equals(FileWatcherJobInfo? other) => ScheduledJob.Id == other?.ScheduledJob.Id;

        /// <inheritdoc />
        public override int GetHashCode() => ScheduledJob.Id.GetHashCode();
    }
}
