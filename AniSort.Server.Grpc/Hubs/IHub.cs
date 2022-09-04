namespace AniSort.Server.Hubs;

public interface IHub<in TKey, TEntity, TUpdate> where TKey : notnull where TUpdate : Enum
{
    /// <summary>
    /// Run the hub and have it propagate updates
    /// </summary>
    /// <param name="cancellationToken">Cancellation token to stop the hub</param>
    Task RunAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Register a listener that stops listening for updates when it's cancellation token is cancelled
    /// </summary>
    /// <param name="listener">Listener to register for updates</param>
    /// <param name="cancellationToken">Cancellation token to unregister the listener after</param>
    void RegisterListener(Func<TEntity, TUpdate, Task> listener, CancellationToken cancellationToken);
    
    /// <summary>
    /// Register a listener that stops listening for updates when it's cancellation token is cancelled
    /// </summary>
    /// <param name="listener">Listener to register for updates</param>
    /// <param name="cancellationToken">Cancellation token to unregister the listener after</param>
    Task RegisterListenerAsync(Func<TEntity, TUpdate, Task> listener, CancellationToken cancellationToken);

    /// <summary>
    /// Register a listener that stops listening for updates when it's cancellation token is cancelled
    /// </summary>
    /// <param name="key">The key of the entity to listen for updates for</param>
    /// <param name="listener">Listener to register for updates</param>
    /// <param name="cancellationToken">Cancellation token to unregister the listener after</param>
    void RegisterListener(TKey key, Func<TEntity, TUpdate, Task> listener, CancellationToken cancellationToken);
    
    /// <summary>
    /// Register a listener that stops listening for updates when it's cancellation token is cancelled
    /// </summary>
    /// <param name="key">The key of the entity to listen for updates for</param>
    /// <param name="listener">Listener to register for updates</param>
    /// <param name="cancellationToken">Cancellation token to unregister the listener after</param>
    Task RegisterListenerAsync(TKey key, Func<TEntity, TUpdate, Task> listener, CancellationToken cancellationToken);

    /// <summary>
    /// Register a listener that stops listening for updates when it's cancellation token is cancelled
    /// </summary>
    /// <param name="predicate">Predicate to filter entries for</param>
    /// <param name="listener">Listener to register for updates</param>
    /// <param name="cancellationToken">Cancellation token to unregister the listener after</param>
    void RegisterListener(Func<TEntity, bool> predicate, Func<TEntity, TUpdate, Task> listener,
        CancellationToken cancellationToken);
    
    /// <summary>
    /// Register a listener that stops listening for updates when it's cancellation token is cancelled
    /// </summary>
    /// <param name="predicate">Predicate to filter entries for</param>
    /// <param name="listener">Listener to register for updates</param>
    /// <param name="cancellationToken">Cancellation token to unregister the listener after</param>
    Task RegisterListenerAsync(Func<TEntity, bool> predicate, Func<TEntity, TUpdate, Task> listener,
        CancellationToken cancellationToken);

    /// <summary>
    /// Publish update for an entity async
    /// </summary>
    /// <param name="entity">Entity to publish</param>
    /// <param name="update">Update type</param>
    /// <returns>Task representing the publish operation</returns>
    Task PublishUpdateAsync(TEntity entity, TUpdate update);
}