using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading.Channels;
using AniSort.Server.DataStructures;

namespace AniSort.Server.Hubs;

public abstract class HubBase<TKey, TEntity, TUpdate> : IHub<TKey, TEntity, TUpdate> where TKey : notnull where TUpdate : Enum
{
    // ReSharper disable once StaticMemberInGenericType
    private static readonly SemaphoreSlim LockSemaphore = new(1, 1);

    private readonly ConcurrentSet<FilterListenerRegistration> filterListeners = new();

    private readonly ConcurrentSet<HubListenerRegistration> globalListeners = new();

    private readonly ConcurrentDictionary<TKey, ConcurrentSet<HubListenerRegistration>> individualListeners = new();

    private readonly ILogger<HubBase<TKey, TEntity, TUpdate>> logger;

    private readonly Channel<(TEntity, TUpdate)> updatesChannel = Channel.CreateUnbounded<(TEntity, TUpdate)>();

    private readonly ActivitySource activitySource;

    private string? hubType = null;

    private string HubType => hubType ??= GetType().Name;

    protected HubBase(ILogger<HubBase<TKey, TEntity, TUpdate>> logger, ActivitySource activitySource)
    {
        this.logger = logger;
        this.activitySource = activitySource;
    }

    protected abstract Func<TEntity, TKey> KeySelector { get; }

    /// <inheritdoc />
    public async Task PublishUpdateAsync(TEntity entity, TUpdate update)
    {
        using var activity = activitySource.StartActivity();
        logger.LogTrace("Publishing update for {Entity}", entity);
        await updatesChannel.Writer.WriteAsync((entity, update));
    }

    /// <inheritdoc />
    public async Task RunAsync(CancellationToken cancellationToken)
    {
        logger.LogTrace("{HubType} Hub starting up", HubType);
        while (!cancellationToken.IsCancellationRequested)
        {
            var (entity, update) = await updatesChannel.Reader.ReadAsync(cancellationToken);

            if (cancellationToken.IsCancellationRequested) break;

            await PublishUpdateInternal(entity, update);
        }
        logger.LogTrace("{HubType} Hub shutting down", HubType);
    }

    private async Task PublishUpdateInternal(TEntity entity, TUpdate update)
    {
        var globalRemovals = new List<HubListenerRegistration>();
        var individualRemovals = new List<(TKey Key, HubListenerRegistration Registration)>();
        var filterRemovals = new List<FilterListenerRegistration>();

        var listeners = new List<ListenerRemoval>();

        await LockSemaphore.WaitAsync();

        try
        {
            listeners.AddRange(globalListeners.Select(r => new ListenerRemoval(r, () => globalRemovals.Add(r))));

            if (individualListeners.TryGetValue(KeySelector(entity), out var individualRegistrations))
            {
                listeners.AddRange(individualRegistrations.Select(r => new ListenerRemoval(r, () => individualRemovals.Add((KeySelector(entity), r)))));
            }

            listeners.AddRange(filterListeners.Where(f => f.Predicate(entity)).Select(r => new ListenerRemoval(r, () => filterRemovals.Add(r))));

            foreach (var removal in listeners)
            {
                if (removal.Registration.CancellationToken.IsCancellationRequested)
                {
                    logger.LogTrace("Slating listener {Listener} for unregistration in {HubType} Hub", removal.Registration, HubType);
                    removal.Remove();
                }
                else
                {
                    logger.LogTrace("Publishing update to listener {Listener} in {HubType} Hub", removal.Registration, HubType);
                    await removal.Registration.Listener(entity, update);
                }
            }

            globalListeners.TryRemoveAll(globalRemovals);
            filterListeners.TryRemoveAll(filterRemovals);
            foreach (var removal in individualRemovals)
            {
                if (individualListeners.TryGetValue(removal.Key, out individualRegistrations))
                {
                    logger.LogTrace("Unregistering listener {Listener} in {HubType} Hub", removal.Registration, HubType);
                    individualRegistrations.TryRemove(removal.Registration);
                }
            }
        }
        finally
        {
            LockSemaphore.Release();
        }
    }

    private record ListenerRemoval(HubListenerRegistration Registration, Action Remove);

    private record HubListenerRegistration(Func<TEntity, TUpdate, Task> Listener, CancellationToken CancellationToken);

    private record FilterListenerRegistration(Func<TEntity, bool> Predicate, Func<TEntity, TUpdate, Task> Listener, CancellationToken CancellationToken) : HubListenerRegistration(Listener, CancellationToken);

    #region Listeners

    /// <inheritdoc />
    public void RegisterListener(Func<TEntity, TUpdate, Task> listener, CancellationToken cancellationToken)
    {
        LockSemaphore.Wait(cancellationToken);
        try
        {
            var registration = new HubListenerRegistration(listener, cancellationToken);
            logger.LogTrace("Adding global listener {Listener} for {HubType} Hub", registration, HubType);
            globalListeners.TryAdd(registration);
        }
        finally
        {
            LockSemaphore.Release();
        }
    }

    /// <inheritdoc />
    public async Task RegisterListenerAsync(Func<TEntity, TUpdate, Task> listener, CancellationToken cancellationToken)
    {
        await LockSemaphore.WaitAsync(cancellationToken);
        try
        {
            var registration = new HubListenerRegistration(listener, cancellationToken);
            logger.LogTrace("Adding global listener {Listener} for {HubType} Hub", registration, HubType);
            globalListeners.TryAdd(registration);
        }
        finally
        {
            LockSemaphore.Release();
        }
    }

    /// <inheritdoc />
    public void RegisterListener(TKey key, Func<TEntity, TUpdate, Task> listener, CancellationToken cancellationToken)
    {
        LockSemaphore.Wait(cancellationToken);
        try
        {
            var keyListeners = individualListeners.GetOrAdd(key, new ConcurrentSet<HubListenerRegistration>());

            var registration = new HubListenerRegistration(listener, cancellationToken);
            logger.LogTrace("Adding individual listener {Listener} for {HubType} Hub", registration, HubType);
            keyListeners.TryAdd(registration);
        }
        finally
        {
            LockSemaphore.Release();
        }
    }

    /// <inheritdoc />
    public async Task RegisterListenerAsync(TKey key, Func<TEntity, TUpdate, Task> listener, CancellationToken cancellationToken)
    {
        await LockSemaphore.WaitAsync(cancellationToken);
        try
        {
            var keyListeners = individualListeners.GetOrAdd(key, new ConcurrentSet<HubListenerRegistration>());

            var registration = new HubListenerRegistration(listener, cancellationToken);
            logger.LogTrace("Adding individual listener {Listener} for {HubType} Hub", registration, HubType);
            keyListeners.TryAdd(registration);
        }
        finally
        {
            LockSemaphore.Release();
        }
    }

    /// <inheritdoc />
    public void RegisterListener(Func<TEntity, bool> predicate, Func<TEntity, TUpdate, Task> listener,
        CancellationToken cancellationToken)
    {
        LockSemaphore.Wait(cancellationToken);
        try
        {
            var registration = new FilterListenerRegistration(predicate, listener, cancellationToken);
            logger.LogTrace("Adding filter listener {Listener} for {HubType} Hub", registration, HubType);
            filterListeners.TryAdd(registration);
        }
        finally
        {
            LockSemaphore.Release();
        }
    }

    /// <inheritdoc />
    public async Task RegisterListenerAsync(Func<TEntity, bool> predicate, Func<TEntity, TUpdate, Task> listener, CancellationToken cancellationToken)
    {
        await LockSemaphore.WaitAsync(cancellationToken);
        try
        {
            var registration = new FilterListenerRegistration(predicate, listener, cancellationToken);
            logger.LogTrace("Adding filter listener {Listener} for {HubType} Hub", registration, HubType);
            filterListeners.TryAdd(registration);
        }
        finally
        {
            LockSemaphore.Release();
        }
    }

    #endregion
}
