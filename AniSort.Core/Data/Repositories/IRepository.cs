using System.Threading.Tasks;
using AniSort.Core.Models;

namespace AniSort.Core.Data;

public interface IRepository<TEntity, in TKey>
{
    TEntity GetById(TKey key);
    Task<TEntity> GetByIdAsync(TKey key);
    void Upsert(TEntity entity);
    Task UpsertAsync(TEntity entity);
    void Remove(TKey key);
    Task RemoveAsync(TKey key);
    void Remove(TEntity entity);
    Task RemoveAsync(TEntity entity);
    void SaveChanges();
    Task SaveChangesAsync();
}
