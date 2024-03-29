﻿using System.Threading.Tasks;
using AniSort.Core.Data.Repositories;
using AniSort.Core.Models;

namespace AniSort.Core.Data.FileStore;

public interface IFileEpisodeRepository : IRepository<EpisodeInfo, int>
{
    public EpisodeInfo GetByAnime(int id, int animeId);
    public Task<EpisodeInfo> GetByAnimeAsync(int id, int animeId);
}
