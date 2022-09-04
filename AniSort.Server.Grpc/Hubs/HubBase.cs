using System.Collections.Concurrent;
using System.Threading.Channels;
using AniSort.Server.DataStructures;

namespace AniSort.Server.Hubs;

public abstract class HubBase<TKey, TEntity, TUpdate> : IHub<TKey, TEntity, TUpdate> where TKey : notnull where TUpdate : Enum
{
    private readonly ConcurrentSet<FilterListenerRegistration> filterListeners = new();

    private readonly ConcurrentSet<HubListenerRegistration> globalListeners = new();

    private readonly ConcurrentDictionary<TKey, ConcurrentSet<HubListenerRegistration>> individualListeners = new();

    private readonly Channel<(TEntity, TUpdate)> updatesChannel = Channel.CreateUnbounded<(TEntity, TUpdate)>();

    private static readonly SemaphoreSlim LockSemaphore = new(1, 1);

    protected abstract Func<TEntity, TKey> KeySelector { get; }

    /// <inheritdoc />
    public async Task PublishUpdateAsync(TEntity entity, TUpdate update)
    {
        await updatesChannel.Writer.WriteAsync((entity, update));
    }

    /// <inheritdoc />
    public async Task RunAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var (entity, update) = await updatesChannel.Reader.ReadAsync(cancellationToken);

            if (cancellationToken.IsCancellationRequested) break;

            await PublishUpdateInternal(entity, update);
        }
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
                    removal.Remove();
                }
                else
                {
                    await removal.Registration.Listener(entity, update);
                }
            }

            globalListeners.TryRemoveAll(globalRemovals);
            filterListeners.TryRemoveAll(filterRemovals);
            foreach (var removal in individualRemovals)
            {
                if (individualListeners.TryGetValue(removal.Key, out individualRegistrations))
                {
                    individualRegistrations.TryRemove(removal.Registration);
                }
            }
        }
        finally
        {
            LockSemaphore.Release();
        }
    }

    #region Listeners

    /// <inheritdoc />
    public void RegisterListener(Func<TEntity, TUpdate, Task> listener, CancellationToken cancellationToken)
    {
        LockSemaphore.Wait(cancellationToken);
        try
        {
            globalListeners.TryAdd(new HubListenerRegistration(listener, cancellationToken));
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
            globalListeners.TryAdd(new HubListenerRegistration(listener, cancellationToken));
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

            keyListeners.TryAdd(new HubListenerRegistration(listener, cancellationToken));
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

            keyListeners.TryAdd(new HubListenerRegistration(listener, cancellationToken));
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
            filterListeners.TryAdd(new FilterListenerRegistration(predicate, listener, cancellationToken));
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
            filterListeners.TryAdd(new FilterListenerRegistration(predicate, listener, cancellationToken));
        }
        finally
        {
            LockSemaphore.Release();
        }
    }

    #endregion

    record ListenerRemoval(HubListenerRegistration Registration, Action Remove);
    
    record HubListenerRegistration(Func<TEntity, TUpdate, Task> Listener, CancellationToken CancellationToken);

    record FilterListenerRegistration(Func<TEntity, bool> Predicate, Func<TEntity, TUpdate, Task> Listener, CancellationToken CancellationToken) : HubListenerRegistration(Listener, CancellationToken);
}
