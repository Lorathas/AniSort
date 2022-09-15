using System.Diagnostics;
using AniSort.Server.Generators;

namespace AniSort.Server.Hubs;

[Hub]
public class ScheduledJobHub : HubBase<Guid, Core.Data.ScheduledJob, HubUpdate>, IScheduledJobHub
{
    /// <inheritdoc />
    public ScheduledJobHub(ILogger<HubBase<Guid, Core.Data.ScheduledJob, HubUpdate>> logger, ActivitySource activitySource) : base(logger, activitySource)
    {
    }

    /// <inheritdoc />
    protected override Func<Core.Data.ScheduledJob, Guid> KeySelector => j => j.Id;
}
