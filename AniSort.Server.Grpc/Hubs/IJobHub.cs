using AniSort.Core.Data;

namespace AniSort.Server.Hubs;

public interface IJobHub : IHub<Guid, Job, JobUpdate>
{
    
}
