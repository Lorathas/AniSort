using AniSort.Core.Data;

namespace AniSort.Server.Hubs;

public class LocalFileHub : HubBase<Guid, LocalFile, HubUpdate>, ILocalFileHub
{
    /// <inheritdoc />
    protected override Func<LocalFile, Guid> KeySelector => f => f.Id;

    /// <inheritdoc />
    public LocalFileHub(ILogger<HubBase<Guid, LocalFile, HubUpdate>> logger) : base(logger)
    {
    }
}
