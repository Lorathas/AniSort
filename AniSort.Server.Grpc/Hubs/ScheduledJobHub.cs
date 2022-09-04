namespace AniSort.Server.Hubs;

public class ScheduledJobHub : HubBase<Guid, Core.Data.ScheduledJob, HubUpdate>, IScheduledJobHub
{
    /// <inheritdoc />
    public ScheduledJobHub(ILogger<HubBase<Guid, Core.Data.ScheduledJob, HubUpdate>> logger) : base(logger)
    {
    }

    /// <inheritdoc />
    protected override Func<Core.Data.ScheduledJob, Guid> KeySelector => j => j.Id;
}
