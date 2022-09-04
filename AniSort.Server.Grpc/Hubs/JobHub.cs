using AniSort.Core.Data;

namespace AniSort.Server.Hubs;

public class JobHub : HubBase<Guid, Job, JobUpdate>, IJobHub
{
    /// <inheritdoc />
    protected override Func<Job, Guid> KeySelector => j => j.Id;
}
