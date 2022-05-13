using System.Threading.Tasks;

namespace AniSort.Core.Data.Repositories;

public interface IRepository<TEntity, in TKey>
{
    /// <summary>
    /// Get entity for the key
    /// </summary>
    /// <param name="key">Primary key to match</param>
    /// <returns>Entity if it exists, otherwise false</returns>
    TEntity GetById(TKey key);

    /// <summary>
    /// Get entity for the key
    /// </summary>
    /// <param name="key">Primary key to match</param>
    /// <returns>Entity if it exists, otherwise false</returns>
    Task<TEntity> GetByIdAsync(TKey key);

    /// <summary>
    /// Add the entity to the repository
    /// </summary>
    /// <param name="entity">Entity to add</param>
    /// <returns>Tracked Entity</returns>
    TEntity Add(TEntity entity);

    /// <summary>
    /// Add the entity to the repository
    /// </summary>
    /// <param name="entity">Entity to add</param>
    /// <returns>Tracked Entity</returns>
    Task<TEntity> AddAsync(TEntity entity);

    /// <summary>
    /// Remove an entity from the repository
    /// </summary>
    /// <param name="key">Key to match the primary key</param>
    /// <returns>The removed entity</returns>
    TEntity Remove(TKey key);

    /// <summary>
    /// Remove an entity from the repository
    /// </summary>
    /// <param name="key">Key to match the primary key</param>
    /// <returns>The removed entity</returns>
    Task<TEntity> RemoveAsync(TKey key);

    /// <summary>
    /// Remove an entity from the repository
    /// </summary>
    /// <param name="entity">Entity to remove</param>
    /// <returns>The removed entity</returns>
    TEntity Remove(TEntity entity);

    /// <summary>
    /// Remove an entity from the repository
    /// </summary>
    /// <param name="entity">Entity to remove</param>
    /// <returns>The removed entity</returns>
    Task<TEntity> RemoveAsync(TEntity entity);

    /// <summary>
    /// Save changes made to the repository
    /// </summary>
    void SaveChanges();

    /// <summary>
    /// Save changes made to the repository
    /// </summary>
    Task SaveChangesAsync();
}
