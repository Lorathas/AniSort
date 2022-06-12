using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AniSort.Core.Data.Repositories;
using AniSort.Core.Models;

namespace AniSort.Core.Data.FileStore;

public class LocalFileEpisodeRepository : IFileEpisodeRepository
{
    private readonly AnimeFileStore animeFileStore;

    public LocalFileEpisodeRepository(AnimeFileStore animeFileStore)
    {
        this.animeFileStore = animeFileStore;
    }

    /// <inheritdoc />
    public EpisodeInfo GetById(int key)
    {
        foreach (var (_, anime) in animeFileStore.Anime)
        {
            foreach (var episode in anime.Episodes)
            {
                if (episode.Id == key)
                {
                    return episode;
                }
            }
        }

        return null;
    }

    /// <inheritdoc />
    public Task<EpisodeInfo> GetByIdAsync(int key)
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
    EpisodeInfo IRepository<EpisodeInfo, int>.Add(EpisodeInfo entity)
    {
        throw new System.NotImplementedException();
    }

    /// <inheritdoc />
    async Task<EpisodeInfo> IRepository<EpisodeInfo, int>.AddAsync(EpisodeInfo entity)
    {
        throw new System.NotImplementedException();
    }

    /// <inheritdoc />
    EpisodeInfo IRepository<EpisodeInfo, int>.Remove(int key)
    {
        throw new System.NotImplementedException();
    }

    /// <inheritdoc />
    async Task<EpisodeInfo> IRepository<EpisodeInfo, int>.RemoveAsync(int key)
    {
        throw new System.NotImplementedException();
    }

    /// <inheritdoc />
    EpisodeInfo IRepository<EpisodeInfo, int>.Remove(EpisodeInfo entity)
    {
        throw new System.NotImplementedException();
    }

    /// <inheritdoc />
    async Task<EpisodeInfo> IRepository<EpisodeInfo, int>.RemoveAsync(EpisodeInfo entity)
    {
        throw new System.NotImplementedException();
    }

    /// <inheritdoc />
    public void Add(EpisodeInfo entity)
    {
        // Leave empty, shouldn't be used
    }

    /// <inheritdoc />
    public async Task AddAsync(EpisodeInfo entity)
    {
        // Leave empty, shouldn't be used
    }

    /// <inheritdoc />
    public EpisodeInfo GetByAnime(int id, int animeId)
    {
        return !animeFileStore.Anime.TryGetValue(animeId, out var anime)
            ? null
            : anime.Episodes.FirstOrDefault(x => x.Id == id);

    }

    /// <inheritdoc />
    public Task<EpisodeInfo> GetByAnimeAsync(int id, int animeId)
    {
        return Task.FromResult(GetByAnime(id, animeId));
    }

    /// <inheritdoc />
    public void Upsert(EpisodeInfo entity)
    {
        if (!animeFileStore.Anime.TryGetValue(entity.AnimeId, out var anime))
        {
            throw new KeyNotFoundException($"No anime found in store for {entity.AnimeId}");
        }
        
        animeFileStore.WriteLock.Wait();
        try
        {
            var existingIndex = anime.Episodes.FindIndex(x => x.Id == entity.Id);

            if (existingIndex != -1)
            {
                anime.Episodes[existingIndex] = entity;
            }
            else
            {
                anime.Episodes.Add(entity);
            }
            animeFileStore.Anime[entity.AnimeId] = anime with { Episodes = anime.Episodes.OrderBy(x => x.Number).ToList() };
        }
        finally
        {
            animeFileStore.WriteLock.Release();
        }
    }

    /// <inheritdoc />
    public async Task UpsertAsync(EpisodeInfo entity)
    {
        if (!animeFileStore.Anime.TryGetValue(entity.AnimeId, out var anime))
        {
            throw new KeyNotFoundException($"No anime found in store for {entity.AnimeId}");
        }
        
        await animeFileStore.WriteLock.WaitAsync();
        try
        {
            var existingIndex = anime.Episodes.FindIndex(x => x.Id == entity.Id);

            if (existingIndex != -1)
            {
                anime.Episodes[existingIndex] = entity;
            }
            else
            {
                anime.Episodes.Add(entity);
            }
            animeFileStore.Anime[entity.AnimeId] = anime with { Episodes = anime.Episodes.OrderBy(x => x.Number).ToList() };
        }
        finally
        {
            animeFileStore.WriteLock.Release();
        }
    }

    /// <inheritdoc />
    public void Remove(int key)
    {
        foreach (var (_, anime) in animeFileStore.Anime)
        {
            int index = anime.Episodes.FindIndex(x => x.Id == key);

            if (index != -1)
            {
                animeFileStore.WriteLock.Wait();
                try
                {
                    anime.Episodes.RemoveAt(index);
                    animeFileStore.Anime[anime.Id] = anime with { Episodes = anime.Episodes.OrderBy(x => x.Number).ToList() };
                }
                finally
                {
                    animeFileStore.WriteLock.Release();
                }
            }
        }
    }

    /// <inheritdoc />
    public async Task RemoveAsync(int key)
    {
        foreach (var (_, anime) in animeFileStore.Anime)
        {
            int index = anime.Episodes.FindIndex(x => x.Id == key);

            if (index != -1)
            {
                await animeFileStore.WriteLock.WaitAsync();
                try
                {
                    anime.Episodes.RemoveAt(index);
                    animeFileStore.Anime[anime.Id] = anime with { Episodes = anime.Episodes.OrderBy(x => x.Number).ToList() };
                }
                finally
                {
                    animeFileStore.WriteLock.Release();
                }
            }
        }
    }

    /// <inheritdoc />
    public void Remove(EpisodeInfo entity)
    {
        if (!animeFileStore.Anime.TryGetValue(entity.AnimeId, out var anime))
        {
            return;
        }

        animeFileStore.WriteLock.Wait();
        try
        {
            int index = anime.Episodes.FindIndex(x => x.Id == entity.Id);
            
            if (index != -1)
            {
                anime.Episodes.RemoveAt(index);
                animeFileStore.Anime[anime.Id] = anime with { Episodes = anime.Episodes.OrderBy(x => x.Number).ToList() };
            }
        }
        finally
        {
            animeFileStore.WriteLock.Release();
        }
    }

    /// <inheritdoc />
    public async Task RemoveAsync(EpisodeInfo entity)
    {
        if (!animeFileStore.Anime.TryGetValue(entity.AnimeId, out var anime))
        {
            return;
        }

        await animeFileStore.WriteLock.WaitAsync();
        try
        {
            int index = anime.Episodes.FindIndex(x => x.Id == entity.Id);
            
            if (index != -1)
            {
                anime.Episodes.RemoveAt(index);
                animeFileStore.Anime[anime.Id] = anime with { Episodes = anime.Episodes.OrderBy(x => x.Number).ToList() };
            }
        }
        finally
        {
            animeFileStore.WriteLock.Release();
        }
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
}
