﻿using System.Threading.Tasks;
using AniSort.Core.Data.Repositories;
using AniSort.Core.Models;

namespace AniSort.Core.Data.FileStore;

public interface IFileFileRepository : IRepository<FileInfo, int>
{
    FileInfo GetByEpisode(int id, int animeId, int episodeId);
    Task<FileInfo> GetByEpisodeAsync(int id, int animeId, int episodeId);
    FileInfo GetByHash(byte[] hash);
    Task<FileInfo> GetByHashAsync(byte[] hash);
    bool ExistsForHash(byte[] hash);
    Task<bool> ExistsForHashAsync(byte[] hash);
}
