using System.Diagnostics;
using AniSort.Core;
using AniSort.Server.Generators;

namespace AniSort.Server.Hubs;

[Hub]
public class SettingsHub : HubBase<int, Config, HubUpdate>, ISettingsHub
{
    /// <inheritdoc />
    public SettingsHub(ILogger<HubBase<int, Config, HubUpdate>> logger, ActivitySource activitySource) : base(logger, activitySource)
    {
    }

    /// <inheritdoc />
    /// Global settings all have ID 1
    protected override Func<Config, int> KeySelector => _ => 1;
}
