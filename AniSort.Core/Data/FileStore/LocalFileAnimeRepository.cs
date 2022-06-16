using System.Threading.Tasks;
using AniSort.Core.Data.Repositories;
using AniSort.Core.Models;

namespace AniSort.Core.Data.FileStore;

public class LocalFileAnimeRepository : IFileAnimeRepository
{
    private readonly AnimeFileStore animeFileStore;

    public LocalFileAnimeRepository(AnimeFileStore animeFileStore)
    {
        this.animeFileStore = animeFileStore;
    }

    /// <inheritdoc />
    public AnimeInfo GetById(int key)
    {
        return animeFileStore.Anime.TryGetValue(key, out var anime) ? anime : default;
    }

    /// <inheritdoc />
    public Task<AnimeInfo> GetByIdAsync(int key)
    {
        return Task.FromResult(GetById(key));
    }

    /// <inheritdoc />
    public bool Exists(int key)
    {
        throw new System.NotImplementedException();
    }

    /// <inheritdoc />
    public async Task<bool> ExistsAsync(int key)
    {
        throw new System.NotImplementedException();
    }

    /// <inheritdoc />
    AnimeInfo IRepository<AnimeInfo, int>.Add(AnimeInfo entity)
    {
        throw new System.NotImplementedException();
    }

    /// <inheritdoc />
    async Task<AnimeInfo> IRepository<AnimeInfo, int>.AddAsync(AnimeInfo entity)
    {
        throw new System.NotImplementedException();
    }

    /// <inheritdoc />
    AnimeInfo IRepository<AnimeInfo, int>.Remove(int key)
    {
        throw new System.NotImplementedException();
    }

    /// <inheritdoc />
    async Task<AnimeInfo> IRepository<AnimeInfo, int>.RemoveAsync(int key)
    {
        throw new System.NotImplementedException();
    }

    /// <inheritdoc />
    AnimeInfo IRepository<AnimeInfo, int>.Remove(AnimeInfo entity)
    {
        throw new System.NotImplementedException();
    }

    /// <inheritdoc />
    async Task<AnimeInfo> IRepository<AnimeInfo, int>.RemoveAsync(AnimeInfo entity)
    {
        throw new System.NotImplementedException();
    }

    /// <inheritdoc />
    public void Add(AnimeInfo entity)
    {
        // Leave empty, shouldn't be used
    }

    /// <inheritdoc />
    public async Task AddAsync(AnimeInfo entity)
    {
        // Leave empty, shouldn't be used
    }

    /// <inheritdoc />
    public void Upsert(AnimeInfo entity)
    {
        animeFileStore.Anime[entity.Id] = entity;
    }

    /// <inheritdoc />
    public Task UpsertAsync(AnimeInfo entity)
    {
        Upsert(entity);

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public void Remove(int key)
    {
        animeFileStore.Anime.TryRemove(key, out _);
    }

    /// <inheritdoc />
    public Task RemoveAsync(int key)
    {
        Remove(key);

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public void Remove(AnimeInfo entity)
    {
        Remove(entity.Id);
    }

    /// <inheritdoc />
    public Task RemoveAsync(AnimeInfo entity)
    {
        Remove(entity);

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public void SaveChanges()
    {
        animeFileStore.Save();
    }

    /// <inheritdoc />
    public async Task SaveChangesAsync()
    {
        await animeFileStore.SaveAsync();
    }

    /// <inheritdoc />
    public void MergeSert(AnimeInfo animeInfo)
    {
        if (animeFileStore.Anime.TryGetValue(animeInfo.Id, out var existing))
        {
            var merged = animeInfo.MergeWith(existing);

            animeFileStore.Anime[animeInfo.Id] = merged;
        }
        else
        {
            animeFileStore.Anime[animeInfo.Id] = animeInfo;
        }
    }
}
