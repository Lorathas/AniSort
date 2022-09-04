using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AniSort.Core.Data.Filtering;
using AniSort.Core.Models;
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
    public IEnumerable<LocalFile> GetForEd2kHash(byte[] hash)
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

    private IOrderedQueryable<LocalFile> SearchForFilesInternal(LocalFileFilter filter)
    {
        string[] searchTerms = filter.Search.Split(' ', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

        var query = Set.AsQueryable();

        query = searchTerms.Aggregate(query, (current, term) => current.Where(f => f.Path.ToLowerInvariant().Contains(term)));

        if (filter.StartTime.HasValue)
        {
            query = query.Where(f => f.CreatedAt >= filter.StartTime.Value || f.UpdatedAt >= filter.StartTime.Value);
        }

        if (filter.EndTime.HasValue)
        {
            query = query.Where(f => f.CreatedAt < filter.EndTime.Value || f.UpdatedAt < filter.EndTime.Value);
        }

        if (filter.Status.HasValue)
        {
            query = query.Where(f => f.Status == filter.Status.Value);
        }
        
        IOrderedQueryable<LocalFile> orderedQuery;

        switch (filter.SortBy)
        {
            case LocalFileSortBy.Path:
                orderedQuery = filter.Sort == SortDirection.Ascending
                    ? query.OrderBy(f => f.Path)
                    : query.OrderByDescending(f => f.Path);
                break;
            case LocalFileSortBy.Length:
                orderedQuery = filter.Sort == SortDirection.Ascending
                    ? query.OrderBy(f => f.FileLength)
                    : query.OrderByDescending(f => f.FileLength);
                break;
            case LocalFileSortBy.Hash:
                orderedQuery = filter.Sort == SortDirection.Ascending
                    ? query.OrderBy(f => f.Ed2kHash)
                    : query.OrderByDescending(f => f.Ed2kHash);
                break;
            case LocalFileSortBy.Status:
                orderedQuery = filter.Sort == SortDirection.Ascending
                    ? query.OrderBy(f => f.Status)
                    : query.OrderByDescending(f => f.Status);
                break;
            case LocalFileSortBy.CreatedAt:
                orderedQuery = filter.Sort == SortDirection.Ascending
                    ? query.OrderBy(f => f.CreatedAt)
                    : query.OrderByDescending(f => f.CreatedAt);
                break;
            case LocalFileSortBy.UpdatedAt:
                orderedQuery = filter.Sort == SortDirection.Ascending
                    ? query.OrderBy(f => f.UpdatedAt)
                    : query.OrderByDescending(f => f.UpdatedAt);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        return orderedQuery;
    }

    private IQueryable<LocalFile> SearchForFilesPagedInternal(LocalFileFilter filter, int pageSize)
    {
        return SearchForFilesInternal(filter)
            .Skip((filter.Page - 1) * pageSize)
            .Take(pageSize);;
    }

    public IEnumerable<LocalFile> SearchForFilesPaged(LocalFileFilter filter, int pageSize)
    {
        return SearchForFilesPagedInternal(filter, pageSize);
    }

    public IAsyncEnumerable<LocalFile> SearchForFilesPagedAsync(LocalFileFilter filter, int pageSize)
    {
        return SearchForFilesPagedInternal(filter, pageSize)
            .AsAsyncEnumerable();
    }

    public Task<int> CountSearchedFilesAsync(LocalFileFilter filter)
    {
        return SearchForFilesInternal(filter).CountAsync();
    }

    public LocalFile GetByIdWithRelated(Guid id)
    {
        return Set.Where(f => f.Id == id)
            .Include(f => f.FileActions)
            .Include(f => f.EpisodeFile)
            .FirstOrDefault();
    }

    public async Task<LocalFile> GetByIdWithRelatedAsync(Guid id)
    {
        return await Set.Where(f => f.Id == id)
            .Include(f => f.FileActions)
            .Include(f => f.EpisodeFile)
            .FirstOrDefaultAsync();
    }
}
