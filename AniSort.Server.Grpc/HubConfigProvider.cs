using AniSort.Core;
using AniSort.Core.Data.Repositories;
using AniSort.Server.Hubs;

namespace AniSort.Server;

public class ConfigHubProvider : IConfigProvider
{
    private Config cached;

    private readonly ISettingsHub hub;

    public ConfigHubProvider(ISettingsHub hub, ISettingsRepository repository)
    {
        this.hub = hub;

        cached = repository.GetSettings()?.Config ?? new Config();

        this.hub.RegisterListener((config, _) =>
        {
            cached = config;
            return Task.CompletedTask;
        }, CancellationToken.None);
    }

    /// <inheritdoc />
    public Config Config => cached;
}
