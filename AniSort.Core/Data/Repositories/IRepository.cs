using System.Threading.Tasks;

namespace AniSort.Core.Data.Repositories;

public interface IRepository<TEntity, in TKey>
{
    TEntity GetById(TKey key);
    Task<TEntity> GetByIdAsync(TKey key);
    void Add(TEntity entity);
    Task AddAsync(TEntity entity);
    void Remove(TKey key);
    Task RemoveAsync(TKey key);
    void Remove(TEntity entity);
    Task RemoveAsync(TEntity entity);
    void SaveChanges();
    Task SaveChangesAsync();
}
