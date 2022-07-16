using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace AniSort.Core.Data.Repositories;

public class LocalFileRepository : RepositoryBase<LocalFile, Guid, AniSortContext>, ILocalFileRepository
{
    /// <inheritdoc />
    public LocalFileRepository(AniSortContext context) : base(context)
    {
    }

    /// <inheritdoc />
    public new LocalFile GetById(Guid key)
    {
        return Set
            .Include(f => f.FileActions)
            .FirstOrDefault(f => f.Id == key);
    }

    /// <inheritdoc />
    public new async Task<LocalFile> GetByIdAsync(Guid key)
    {
        return await Set
            .Include(f => f.FileActions)
            .FirstOrDefaultAsync(f => f.Id == key);
    }

    /// <inheritdoc />
    public LocalFile GetFirstForEd2kHash(byte[] hash)
    {
        return Set.FirstOrDefault(f => f.Ed2kHash == hash);
    }

    /// <inheritdoc />
    public async Task<LocalFile> GetFirstForEd2kHashAsync(byte[] hash)
    {
        return await Set.FirstOrDefaultAsync(f => f.Ed2kHash == hash);
    }

    /// <inheritdoc />
    public IQueryable<LocalFile> GetForEd2kHash(byte[] hash)
    {
        return Set.Where(f => f.Ed2kHash == hash);
    }

    /// <inheritdoc />
    public IAsyncEnumerable<LocalFile> GetForEd2kHashAsync(byte[] hash)
    {
        return Set.Where(f => f.Ed2kHash == hash).AsAsyncEnumerable();
    }

    /// <inheritdoc />
    public bool ExistsForEd2kHash(byte[] hash)
    {
        return Set.Any(f => f.Ed2kHash == hash);
    }

    /// <inheritdoc />
    public async Task<bool> ExistsForEd2kHashAsync(byte[] hash)
    {
        return await Set.AnyAsync(f => f.Ed2kHash == hash);
    }

    /// <inheritdoc />
    public LocalFile GetForPath(string path)
    {
        return Set.FirstOrDefault(f => f.Path == path);
    }

    /// <inheritdoc />
    public async Task<LocalFile> GetForPathAsync(string path)
    {
        return await Set.FirstOrDefaultAsync(f => f.Path == path);
    }

    /// <inheritdoc />
    public bool ExistsForPath(string path)
    {
        return Set.Any(f => f.Path == path);
    }

    /// <inheritdoc />
    public async Task<bool> ExistsForPathAsync(string path)
    {
        return await Set.AnyAsync(f => f.Path == path);
    }

    /// <inheritdoc />
    public IEnumerable<LocalFile> GetWithoutResolution()
    {
        return Set.Where(f => f.Path.Contains("[0x0]"))
            .Include(f => f.EpisodeFile);
    }

    /// <inheritdoc />
    public IAsyncEnumerable<LocalFile> GetWithoutResolutionAsync()
    {
        return Set.Where(f => f.Path.Contains("[0x0]"))
            .Include(f => f.EpisodeFile)
            .AsAsyncEnumerable();
    }

    /// <inheritdoc />
    public IQueryable<LocalFile> GetOtherLocalFilesForSameSeriesAsFile(Guid localFileId)
    {
        return (from originalFile in Set
            join originalEpisodeFile in Context.EpisodeFiles
                on originalFile.EpisodeFileId equals originalEpisodeFile.Id
            join originalEpisode in Context.Episodes
                on originalEpisodeFile.EpisodeId equals originalEpisode.Id
            join anime in Context.Anime
                on originalEpisode.AnimeId equals anime.Id
            join destinationEpisodes in Context.Episodes
                on anime.Id equals destinationEpisodes.AnimeId
            join destinationEpisodeFiles in Context.EpisodeFiles
                on destinationEpisodes.Id equals destinationEpisodeFiles.EpisodeId
            join df in Context.LocalFiles
                on destinationEpisodes.Id equals df.EpisodeFileId
            where originalFile.Id == localFileId && df.Id != localFileId
            orderby destinationEpisodes.Number
            select df)
            .Include(f => f.EpisodeFile)
            .ThenInclude(ef => ef.Episode)
            .ThenInclude(e => e.Anime);
    }

    /// <inheritdoc />
    public IAsyncEnumerable<LocalFile> GetOtherLocalFilesForSameSeriesAsFileAsync(Guid localFileId)
    {
        return (from originalFile in Set
                join originalEpisodeFile in Context.EpisodeFiles
                    on originalFile.EpisodeFileId equals originalEpisodeFile.Id
                join originalEpisode in Context.Episodes
                    on originalEpisodeFile.EpisodeId equals originalEpisode.Id
                join anime in Context.Anime
                    on originalEpisode.AnimeId equals anime.Id
                join destinationEpisodes in Context.Episodes
                    on anime.Id equals destinationEpisodes.AnimeId
                join destinationEpisodeFiles in Context.EpisodeFiles
                    on destinationEpisodes.Id equals destinationEpisodeFiles.EpisodeId
                join df in Context.LocalFiles
                    on destinationEpisodes.Id equals df.EpisodeFileId
                where originalFile.Id == localFileId && df.Id != localFileId
                orderby destinationEpisodes.Number
                select df)
            .Include(f => f.EpisodeFile)
            .ThenInclude(ef => ef.Episode)
            .ThenInclude(e => e.Anime)
            .AsAsyncEnumerable();
    }
}
