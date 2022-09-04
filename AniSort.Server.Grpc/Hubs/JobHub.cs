using AniSort.Core.Data;
using AniSort.Server.Generators;

namespace AniSort.Server.Hubs;

[Hub]
public class JobHub : HubBase<Guid, Job, JobUpdate>, IJobHub
{
    /// <inheritdoc />
    protected override Func<Job, Guid> KeySelector => j => j.Id;

    /// <inheritdoc />
    public JobHub(ILogger<HubBase<Guid, Job, JobUpdate>> logger) : base(logger)
    {
    }
}
