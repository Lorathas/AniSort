using System.Collections.Concurrent;
using AniSort.Core;
using AniSort.Core.Commands;
using AniSort.Core.Data;
using AniSort.Core.Data.Repositories;
using AniSort.Core.DataFlow;
using AniSort.Core.Extensions;
using AniSort.Server.Extensions;
using AniSort.Server.Hubs;
using Google.Protobuf.WellKnownTypes;

namespace AniSort.Server.HostedServices;

public class PathWatcherService : BackgroundService
{
    private readonly ConcurrentDictionary<string, FileSystemWatcher> watchers = new();

    private readonly ILogger<PathWatcherService> log;

    private readonly ISettingsHub settingsHub;

    private readonly IJobHub jobHub;

    private readonly ISettingsRepository settingsRepository;

    private readonly IJobFactory jobFactory;

    private readonly IServiceProvider serviceProvider;

    /// <inheritdoc />
    public PathWatcherService(ISettingsHub settingsHub, ILogger<PathWatcherService> log, ISettingsRepository settingsRepository, IJobHub jobHub, IJobFactory jobFactory, IServiceProvider serviceProvider)
    {
        this.settingsHub = settingsHub;
        this.log = log;
        this.jobHub = jobHub;
        this.jobFactory = jobFactory;
        this.serviceProvider = serviceProvider;
        this.settingsRepository = settingsRepository;
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var settings = await settingsRepository.GetSettingsDetachedAsync();

        if (settings != null)
        {
            await UpdateWatchersFromSettingsAsync(settings.Config, HubUpdate.Initial);
            
            
        }

        await settingsHub.RegisterListenerAsync(UpdateWatchersFromSettingsAsync, stoppingToken);

        await stoppingToken;

        foreach (var watcher in watchers.Values)
        {
            watcher.Dispose();
        }
    }

    private Task UpdateWatchersFromSettingsAsync(Config config, HubUpdate update)
    {
        var existing = new HashSet<string>();
        
        foreach (string path in config.Sources)
        {
            if (!Directory.Exists(path))
            {
                log.LogError("Source file directory doesn't exist: {SourcePath}", path);
                continue;
            }

            watchers.AddOrUpdate(path, CreateWatchersForPath, (_, oldWatcher) => oldWatcher);

            existing.Add(path);
        }


        var forRemoval = watchers.Keys.Except(existing);
        
        foreach (string path in forRemoval)
        {
            if (watchers.TryRemove(path, out var watcher))
            {
                watcher.Dispose();
            }
        }
        
        return Task.CompletedTask;
    }

    private void OnFileChanged(object sender, FileSystemEventArgs args)
    {
        using var scope = serviceProvider.CreateAsyncScope();
        var jobRepository = scope.ServiceProvider.GetService<IJobRepository>() ?? throw new ArgumentNullException($"Cannot instantiate {nameof(IJobRepository)}");
        
        string name;
        if (sender is FileSystemWatcher watcher)
        {
            name = $"Directory watcher {watcher.Path} file changed: {args.Name}";
        }
        else
        {
            name = $"Directory watcher file changed: {args.FullPath}";
        }

        var job = jobFactory.CreateHashFileJob(name, args.FullPath);

        jobRepository.Add(job);
        jobRepository.SaveChangesAsync();
        jobRepository.Detach(job);

        jobHub.PublishUpdateAsync(job, JobUpdate.JobCreated).Wait();
    }

    private FileSystemWatcher CreateWatchersForPath(string path)
    {
        var watcher = new FileSystemWatcher(path);
        FileImportExtensions.SupportedFileExtensions.Select(ext => $"*.{ext}").ToList().ForEach(ext => watcher.Filters.Add(ext));

        watcher.NotifyFilter = NotifyFilters.Attributes | NotifyFilters.Security | NotifyFilters.Size | NotifyFilters.CreationTime | NotifyFilters.DirectoryName | NotifyFilters.FileName | NotifyFilters.LastAccess | NotifyFilters.LastWrite;

        watcher.Changed += OnFileChanged;
        watcher.Created += OnFileChanged;
        // watcher.Renamed += OnFileChanged;

        watcher.IncludeSubdirectories = true;
        watcher.EnableRaisingEvents = true;

        return watcher;
    }
}
