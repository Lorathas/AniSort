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
    public void Add(TEntity entity)
    {
        Set.Add(entity);
    }

    public async Task AddAsync(TEntity entity)
    {
        await Set.AddAsync(entity);
    }

    /// <inheritdoc />
    public void Remove(TKey key)
    {
        var entity = GetById(key);
        if (entity != null)
        {
            Set.Remove(entity);
        }
    }

    /// <inheritdoc />
    public async Task RemoveAsync(TKey key)
    {
        var entity = await GetByIdAsync(key);
        if (entity != null)
        {
            Set.Remove(entity);
        }
    }

    /// <inheritdoc />
    public void Remove(TEntity entity)
    {
        Set.Remove(entity);
    }

    /// <inheritdoc />
    public Task RemoveAsync(TEntity entity)
    {
        Set.Remove(entity);
        return Task.CompletedTask;
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
