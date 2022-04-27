﻿using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using AniSort.Core.Models;

namespace AniSort.Core.Data;

public class LocalAnimeRepository : IAnimeRepository
{
    private readonly AnimeFileStore animeFileStore;

    public LocalAnimeRepository(AnimeFileStore animeFileStore)
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
}