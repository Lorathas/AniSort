using AniSort.Core.Data;

namespace AniSort.Server.Hubs;

public interface ILocalFileHub : IHub<Guid, LocalFile, HubUpdate>
{
    
}
