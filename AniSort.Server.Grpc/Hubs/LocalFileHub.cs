using System.Diagnostics;
using AniSort.Core.Data;
using AniSort.Server.Generators;

namespace AniSort.Server.Hubs;

[Hub]
public class LocalFileHub : HubBase<Guid, LocalFile, HubUpdate>, ILocalFileHub
{
    /// <inheritdoc />
    protected override Func<LocalFile, Guid> KeySelector => f => f.Id;

    /// <inheritdoc />
    public LocalFileHub(ILogger<HubBase<Guid, LocalFile, HubUpdate>> logger, ActivitySource activitySource) : base(logger, activitySource)
    {
    }
}
