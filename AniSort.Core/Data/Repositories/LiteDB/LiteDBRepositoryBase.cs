using System.Collections.Generic;
using System.Threading.Tasks;
using LiteDB;

namespace AniSort.Core.Data.Repositories.LiteDB;

// ReSharper disable once InconsistentNaming
public abstract class LiteDBRepositoryBase<TEntity> : IRepository<TEntity, ObjectId>
{
    protected readonly LiteDatabase Database;
    protected readonly ILiteCollection<TEntity> Collection;

    protected LiteDBRepositoryBase(LiteDatabase database)
    {
        Database = database;
        Collection = Database.GetCollection<TEntity>(ColName);
    }
    
    protected abstract string ColName { get; }

    protected abstract ObjectId GetObjectId(TEntity entity);

    public void Dispose()
    {
        Database.Dispose();
    }

    public ValueTask DisposeAsync()
    {
        Database.Dispose();
        return ValueTask.CompletedTask;
    }

    public TEntity? GetById(ObjectId key)
    {
        return Collection.FindById(key);
    }

    public Task<TEntity?> GetByIdAsync(ObjectId key)
    {
        return Task.FromResult(Collection.FindById(key));
    }

    public bool Exists(ObjectId key)
    {
        return Collection.Exists(Query.EQ("_id", key));
    }

    public Task<bool> ExistsAsync(ObjectId key)
    {
        return Task.FromResult(Exists(key));
    }

    public TEntity Add(TEntity entity)
    {
        Collection.Insert(entity);

        return entity;
    }

    public Task<TEntity> AddAsync(TEntity entity)
    {
        return Task.FromResult(Add(entity));
    }

    public TEntity Upsert(TEntity entity)
    {
        Collection.Upsert(entity);

        return entity;
    }

    public Task<TEntity> UpsertAsync(TEntity entity) => Task.FromResult(Upsert(entity));

    public TEntity UpsertAndDetach(TEntity entity) => Upsert(entity);

    public async Task<TEntity> UpsertAndDetachAsync(TEntity entity) => await UpsertAsync(entity);

    public TEntity? Remove(ObjectId key)
    {
        var entity = Collection.FindById(key);
        
        Collection.Delete(key);

        return entity;
    }

    public Task<TEntity?> RemoveAsync(ObjectId key) => Task.FromResult(Remove(key));

    public TEntity Remove(TEntity entity)
    {
        var id = GetObjectId(entity);

        Collection.Delete(id);

        return entity;
    }

    public Task<TEntity> RemoveAsync(TEntity entity) => Task.FromResult(Remove(entity));

    public void SaveChanges()
    {
        // Do nothing, saves happen automatically
    }

    public Task SaveChangesAsync()
    {
        // Do nothing, saves happen automatically
        return Task.CompletedTask;
    }

    public void Detach(TEntity entity)
    {
        // Do nothing, detach doesn't apply to LiteDB
    }

    public void Detach(IEnumerable<TEntity> entities)
    {
        // Do nothing, detach doesn't apply to LiteDB
    }
}