using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AniSort.Core.Data.Repositories;
using AniSort.Core.Models;

namespace AniSort.Core.Data.FileStore;

public class LocalFileFileRepository : IFileFileRepository
{
    private readonly AnimeFileStore animeFileStore;

    public LocalFileFileRepository(AnimeFileStore animeFileStore)
    {
        this.animeFileStore = animeFileStore;
    }

    /// <inheritdoc />
    public FileInfo GetById(int key)
    {
        foreach (var (_, anime) in animeFileStore.Anime)
        {
            foreach (var episode in anime.Episodes)
            {
                foreach (var file in episode.Files)
                {
                    if (file.Id == key)
                    {
                        return file;
                    }
                }
            }
        }

        return null;
    }

    /// <inheritdoc />
    public Task<FileInfo> GetByIdAsync(int key)
    {
        return Task.FromResult(GetById(key));
    }

    /// <inheritdoc />
    public bool Exists(int key)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public async Task<bool> ExistsAsync(int key)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public void Add(FileInfo entity)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    FileInfo IRepository<FileInfo, int>.Add(FileInfo entity)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    async Task<FileInfo> IRepository<FileInfo, int>.AddAsync(FileInfo entity)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    FileInfo IRepository<FileInfo, int>.Remove(int key)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    async Task<FileInfo> IRepository<FileInfo, int>.RemoveAsync(int key)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    FileInfo IRepository<FileInfo, int>.Remove(FileInfo entity)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    async Task<FileInfo> IRepository<FileInfo, int>.RemoveAsync(FileInfo entity)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public Task AddAsync(FileInfo entity)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public FileInfo GetByEpisode(int id, int animeId, int episodeId)
    {
        if (!animeFileStore.Anime.TryGetValue(animeId, out var anime))
        {
            return default;
        }

        var episode = anime.Episodes.FirstOrDefault(x => x.Id == episodeId);

        return episode?.Files.FirstOrDefault(x => x.Id == id);
    }

    /// <inheritdoc />
    public Task<FileInfo> GetByEpisodeAsync(int id, int animeId, int episodeId)
    {
        return Task.FromResult(GetByEpisode(id, animeId, episodeId));
    }

    /// <inheritdoc />
    public FileInfo GetByHash(byte[] hash)
    {
        return animeFileStore.Files.TryGetValue(hash, out var file) ? file : null;
    }

    /// <inheritdoc />
    public Task<FileInfo> GetByHashAsync(byte[] hash)
    {
        return Task.FromResult(animeFileStore.Files.TryGetValue(hash, out var file) ? file : null);
    }

    /// <inheritdoc />
    public bool ExistsForHash(byte[] hash)
    {
        return animeFileStore.Files.ContainsKey(hash);
    }

    /// <inheritdoc />
    public Task<bool> ExistsForHashAsync(byte[] hash)
    {
        return Task.FromResult(animeFileStore.Files.ContainsKey(hash));
    }
    
    public void Upsert(FileInfo entity)
    {
        if (!animeFileStore.Anime.TryGetValue(entity.AnimeId, out var anime))
        {
            throw new KeyNotFoundException($"No anime found for id {entity.AnimeId}");
        }

        var episode = anime.Episodes.FirstOrDefault(x => x.Id == entity.EpisodeId);

        if (episode == default)
        {
            throw new KeyNotFoundException($"No episode found for id {entity.EpisodeId} in anime {entity.AnimeId}");
        }

        int index = episode.Files.FindIndex(x => x.Id == entity.Id);

        animeFileStore.WriteLock.Wait();
        try
        {
            entity = entity with { Episode = episode };
            if (index == -1)
            {
                episode.Files.Add(entity);
            }
            else
            {
                episode.Files[index] = entity;
            }
            
        }
        finally
        {
            animeFileStore.WriteLock.Release();
        }
    }
    
    public async Task UpsertAsync(FileInfo entity)
    {
        if (!animeFileStore.Anime.TryGetValue(entity.AnimeId, out var anime))
        {
            throw new KeyNotFoundException($"No anime found for id {entity.AnimeId}");
        }

        var episode = anime.Episodes.FirstOrDefault(x => x.Id == entity.EpisodeId);

        if (episode == default)
        {
            throw new KeyNotFoundException($"No episode found for id {entity.EpisodeId} in anime {entity.AnimeId}");
        }

        int index = episode.Files.FindIndex(x => x.Id == entity.Id);

        await animeFileStore.WriteLock.WaitAsync();
        try
        {
            entity = entity with { Episode = episode };
            if (index == -1)
            {
                episode.Files.Add(entity);
            }
            else
            {
                episode.Files[index] = entity;
            }
            
        }
        finally
        {
            animeFileStore.WriteLock.Release();
        }
    }

    /// <inheritdoc />
    public void Remove(int key)
    {
        animeFileStore.WriteLock.Wait();
        try
        {
            foreach (var (_, anime) in animeFileStore.Anime)
            {
                foreach (var episode in anime.Episodes)
                {
                    foreach (var file in episode.Files)
                    {
                        if (file.Id == key)
                        {
                            episode.Files.Remove(file);
                            animeFileStore.Files.TryRemove(file.Ed2kHash, out _);
                            break;
                        }
                    }
                }
            }
        }
        finally
        {
            animeFileStore.WriteLock.Release();
        }
    }

    /// <inheritdoc />
    public async Task RemoveAsync(int key)
    {
        await animeFileStore.WriteLock.WaitAsync();
        try
        {
            foreach (var (_, anime) in animeFileStore.Anime)
            {
                foreach (var episode in anime.Episodes)
                {
                    foreach (var file in episode.Files)
                    {
                        if (file.Id == key)
                        {
                            episode.Files.Remove(file);
                            animeFileStore.Files.TryRemove(file.Ed2kHash, out _);
                            break;
                        }
                    }
                }
            }
        }
        finally
        {
            animeFileStore.WriteLock.Release();
        }
    }

    /// <inheritdoc />
    public void Remove(FileInfo entity)
    {
        animeFileStore.WriteLock.Wait();
        try
        {
            if (animeFileStore.Anime.TryGetValue(entity.AnimeId, out var anime))
            {
                var episode = anime.Episodes.FirstOrDefault(x => x.Id == entity.EpisodeId);

                if (episode != default)
                {
                    int index = episode.Files.FindIndex(x => x.Id == entity.Id);

                    if (index != -1)
                    {
                        episode.Files.RemoveAt(index);
                    }
                }
            }
            animeFileStore.Files.TryRemove(entity.Ed2kHash, out _);
        }
        finally
        {
            animeFileStore.WriteLock.Release();
        }
    }

    /// <inheritdoc />
    public async Task RemoveAsync(FileInfo entity)
    {
        await animeFileStore.WriteLock.WaitAsync();
        try
        {
            if (animeFileStore.Anime.TryGetValue(entity.AnimeId, out var anime))
            {
                var episode = anime.Episodes.FirstOrDefault(x => x.Id == entity.EpisodeId);

                if (episode != default)
                {
                    int index = episode.Files.FindIndex(x => x.Id == entity.Id);

                    if (index != -1)
                    {
                        episode.Files.RemoveAt(index);
                    }
                }
            }
            animeFileStore.Files.TryRemove(entity.Ed2kHash, out _);
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

    /// <inheritdoc />
    public void Detach(FileInfo entity)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public void Detach(IEnumerable<FileInfo> entities)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        throw new NotImplementedException();
    }
}
