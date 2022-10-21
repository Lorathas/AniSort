﻿using System.Collections.Concurrent;
using System.Threading.Tasks.Dataflow;
using AniSort.Core.Commands;
using AniSort.Core.Data;
using AniSort.Core.Data.Repositories;
using AniSort.Core.DataFlow;
using AniSort.Core.Exceptions;
using AniSort.Server.Extensions;
using AniSort.Server.Hubs;

namespace AniSort.Server.HostedServices;

public class JobRunnerService : BackgroundService
{
    private readonly IJobHub jobHub;

    private readonly IServiceProvider serviceProvider;

    private readonly HashCommand hashCommand;

    private readonly SortCommand sortCommand;

    private ConcurrentDictionary<Core.Data.JobType, ITargetBlock<Job>> jobQueues = new();

    /// <inheritdoc />
    public JobRunnerService(IJobHub jobHub, IServiceProvider serviceProvider, HashCommand hashCommand, SortCommand sortCommand)
    {
        this.jobHub = jobHub;
        this.serviceProvider = serviceProvider;
        this.hashCommand = hashCommand;
        this.sortCommand = sortCommand;
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var hashPipeline = hashCommand.BuildPipeline(stoppingToken);
        var sortPipeline = sortCommand.BuildPipeline(stoppingToken);
        
        jobQueues[Core.Data.JobType.HashFile] = hashPipeline;
        jobQueues[Core.Data.JobType.SortFile] = sortPipeline;
        
        
        List<Job> unstarted;
        await using (var jobRepository = serviceProvider.GetService<IJobRepository>())
        {
            unstarted = await jobRepository!.GetPendingJobs().ToListAsync(cancellationToken: stoppingToken);
        }

        if (stoppingToken.IsCancellationRequested)
        {
            return;
        }

        foreach (var job in unstarted)
        {
            await QueueJobAsync(job, JobUpdate.JobCreated, stoppingToken);
        }

        await jobHub.RegisterListenerAsync(async (job, update) =>
        {
            if (update != JobUpdate.JobCreated)
            {
                return;
            }

            await QueueJobAsync(job, update, stoppingToken);
        }, stoppingToken);

        await stoppingToken;
    }

    private async Task QueueJobAsync(Job job, JobUpdate update, CancellationToken cancellationToken)
    {
        if (jobQueues.TryGetValue(job.Type, out var block))
        {
            await block.SendAsync(job, cancellationToken);
        }
        else
        {
            throw new UnsupportedJobTypeException();
        }
    }
}
