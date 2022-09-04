namespace AniSort.Server.Hubs;

public interface IScheduledJobHub : IHub<Guid, Core.Data.ScheduledJob, HubUpdate>
{
    
}
