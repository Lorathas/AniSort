using System.Diagnostics;
using System.Threading.Channels;
using AniSort.Core;
using AniSort.Core.Data;
using AniSort.Core.Data.Repositories;
using AniSort.Server.Data.Settings;
using AniSort.Server.Extensions;
using AniSort.Server.Hubs;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;

namespace AniSort.Server.Services;

public class SettingsService : Data.Settings.SettingsService.SettingsServiceBase
{
    private readonly ISettingsHub settingsHub;

    private readonly ISettingsRepository settingsRepository;

    private readonly ILogger<SettingsService> logger;

    /// <inheritdoc />
    public SettingsService(ISettingsRepository settingsRepository, ILogger<SettingsService> logger, ISettingsHub settingsHub)
    {
        this.settingsRepository = settingsRepository;
        this.logger = logger;
        this.settingsHub = settingsHub;
    }

    /// <inheritdoc />
    public override async Task<SettingsReply> GetSettings(Empty request, ServerCallContext context)
    {
        var settings = await settingsRepository.GetSettingsAsync();

        if (settings == null)
        {
            throw new RpcException(new Status(StatusCode.NotFound, "Settings do not exist in the database yet"));
        }

        return settings.Config.ToReply();
    }

    /// <inheritdoc />
    public override async Task<SettingsReply> SaveSettings(SettingsReply request, ServerCallContext context)
    {
        var config = request.ToModel();

        var settings = new Setting { Id = 1, Config = config };

        config = (await settingsRepository.UpsertSettingsAsync(settings)).Config;

        await settingsHub.PublishUpdateAsync(config, HubUpdate.ItemUpdated);
        
        return config.ToReply();
    }

    /// <inheritdoc />
    public override async Task ActiveUpdates(IAsyncStreamReader<SettingsReply> requestStream, IServerStreamWriter<SettingsReply> responseStream, ServerCallContext context)
    {
        async Task SendSettingsAsync(Config config, HubUpdate _) => await responseStream.WriteAsync(config.ToReply());
        
        await settingsHub.RegisterListenerAsync(SendSettingsAsync, context.CancellationToken);
        
        var settings = await settingsRepository.GetSettingsAsync();

        if (settings != null)
        {
            await SendSettingsAsync(settings.Config, HubUpdate.Initial);
        }
        else
        {
            await SendSettingsAsync(new Config(), HubUpdate.Initial);
        }

        while (!context.CancellationToken.IsCancellationRequested)
        {
            await requestStream.MoveNext();
            
            var config = requestStream.Current.ToModel();

            settings = new Setting { Id = 1, Config = config };

            config = (await settingsRepository.UpsertSettingsAsync(settings)).Config;

            await settingsHub.PublishUpdateAsync(config, HubUpdate.ItemUpdated);
        }
    }
}
