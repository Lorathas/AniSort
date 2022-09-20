using AniSort.Core.Data;
using AniSort.Core.DataFlow;
using AniSort.Server.Extensions;
using AniSort.Server.Hubs;

namespace AniSort.Server.Jobs;

public class JobHubUpdateProvider : IJobUpdateProvider
{
    private readonly IJobHub jobHub;

    public JobHubUpdateProvider(IJobHub jobHub)
    {
        this.jobHub = jobHub;
    }

    /// <inheritdoc />
    public void UpdateJobStatus(Job job)
    {
        jobHub.PublishUpdateAsync(job, job.ToJobUpdate()).Wait();
    }

    /// <inheritdoc />
    public async Task UpdateJobStatusAsync(Job job)
    {
        await jobHub.PublishUpdateAsync(job, job.ToJobUpdate());
    }
}
