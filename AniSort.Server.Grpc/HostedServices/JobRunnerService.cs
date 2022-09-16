using System.Collections.Concurrent;
using System.Threading.Tasks.Dataflow;
using AniSort.Core.Data;
using AniSort.Core.Data.Repositories;
using AniSort.Server.Hubs;

namespace AniSort.Server.HostedServices;

public class JobRunnerService : BackgroundService
{
    private readonly IJobHub jobHub;

    private readonly IServiceProvider serviceProvider;

    private ConcurrentDictionary<JobType, ITargetBlock<Job>> jobQueues = new();

    /// <inheritdoc />
    public JobRunnerService(IJobHub jobHub, IServiceProvider serviceProvider)
    {
        this.jobHub = jobHub;
        this.serviceProvider = serviceProvider;
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
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
            
        }
        
        throw new NotImplementedException();
    }
}
