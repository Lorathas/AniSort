using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace AniSort.Core.Data.Repositories;

public abstract class RepositoryBase<TEntity, TKey, TContext> : IRepository<TEntity, TKey>, IDisposable, IAsyncDisposable
    where TContext : DbContext
    where TEntity : class
{
    protected readonly TContext Context;
    protected readonly DbSet<TEntity> Set;

    protected RepositoryBase(TContext context)
    {
        Context = context;
        Set = context.Set<TEntity>();
    }

    /// <inheritdoc />
    public TEntity GetById(TKey key)
    {
        return Set.Find(key);
    }

    /// <inheritdoc />
    public async Task<TEntity> GetByIdAsync(TKey key)
    {
        return await Set.FindAsync(key);
    }

    /// <inheritdoc />
    public TEntity Add(TEntity entity)
    {
        return Set.Add(entity).Entity;
    }

    public async Task<TEntity> AddAsync(TEntity entity)
    {
        return (await Set.AddAsync(entity)).Entity;
    }

    /// <inheritdoc />
    public TEntity Remove(TKey key)
    {
        var entity = GetById(key);
        if (entity != null)
        {
            return Set.Remove(entity).Entity;
        }
        return null;
    }

    /// <inheritdoc />
    public async Task<TEntity> RemoveAsync(TKey key)
    {
        var entity = await GetByIdAsync(key);
        if (entity != null)
        {
            return Set.Remove(entity).Entity;
        }

        return null;
    }

    /// <inheritdoc />
    public TEntity Remove(TEntity entity)
    {
        return Set.Remove(entity).Entity;
    }

    /// <inheritdoc />
    public Task<TEntity> RemoveAsync(TEntity entity)
    {
        return Task.FromResult(Set.Remove(entity).Entity);
    }

    /// <inheritdoc />
    public void SaveChanges()
    {
        Context.SaveChanges();
    }

    /// <inheritdoc />
    public async Task SaveChangesAsync()
    {
        await Context.SaveChangesAsync();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Context?.Dispose();
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        return Context?.DisposeAsync() ?? ValueTask.CompletedTask;
    }
}
