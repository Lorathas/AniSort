﻿using System;
using System.Collections.Generic;
using System.Linq;
using AniSort.Core.Data.Filtering;
using Microsoft.EntityFrameworkCore;

namespace AniSort.Core.Data.Repositories;

public class JobRepository : RepositoryBase<Job, Guid, AniSortContext>, IJobRepository
{
    public JobRepository(AniSortContext context) : base(context)
    {
    }

    private IOrderedQueryable<Job> GetFilteredJobsInternal(JobFilter filter)
    {
        var query = Set.AsQueryable();

        if (filter.Status.HasValue)
        {
            query = query.Where(j => j.Status == filter.Status.Value);
        }

        if (filter.Type.HasValue)
        {
            query = query.Where(j => j.Type == filter.Type.Value);
        }

        if (filter.StartTime.HasValue)
        {
            query = query.Where(j =>
                j.QueuedAt >= filter.StartTime.Value
                || j.StartedAt >= filter.StartTime.Value
                || j.CompletedAt >= filter.StartTime.Value);
        }
        
        if (filter.EndTime.HasValue)
        {
            query = query.Where(j =>
                j.QueuedAt >= filter.EndTime.Value
                || j.StartedAt >= filter.EndTime.Value
                || j.CompletedAt >= filter.EndTime.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.Name))
        {
            query = query.Where(j => j.Name.Contains(filter.Name));
        }

        IOrderedQueryable<Job> orderedQuery;
        switch (filter.SortBy)
        {
            case JobFilterSortBy.Id:
                orderedQuery = filter.Sort == SortDirection.Ascending
                    ? query.OrderBy(j => j.Id)
                    : query.OrderByDescending(j => j.Id);
                break;
            case JobFilterSortBy.Name:
                orderedQuery = filter.Sort == SortDirection.Ascending
                    ? query.OrderBy(j => j.Name)
                    : query.OrderByDescending(j => j.Name);
                break;
            case JobFilterSortBy.Type:
                orderedQuery = filter.Sort == SortDirection.Ascending
                    ? query.OrderBy(j => j.Type)
                    : query.OrderByDescending(j => j.Type);
                break;
            case JobFilterSortBy.Status:
                orderedQuery = filter.Sort == SortDirection.Ascending
                    ? query.OrderBy(j => j.Status)
                    : query.OrderByDescending(j => j.Status);
                break;
            case JobFilterSortBy.PercentComplete:
                orderedQuery = filter.Sort == SortDirection.Ascending
                    ? query.OrderBy(j => j.PercentComplete)
                    : query.OrderByDescending(j => j.PercentComplete);
                break;
            case JobFilterSortBy.IsFinished:
                orderedQuery = filter.Sort == SortDirection.Ascending
                    ? query.OrderBy(j => j.IsFinished)
                    : query.OrderByDescending(j => j.IsFinished);
                break;
            case JobFilterSortBy.QueuedAt:
                orderedQuery = filter.Sort == SortDirection.Ascending
                    ? query.OrderBy(j => j.QueuedAt)
                    : query.OrderByDescending(j => j.QueuedAt);
                break;
            case JobFilterSortBy.StartedAt:
                orderedQuery = filter.Sort == SortDirection.Ascending
                    ? query.OrderBy(j => j.StartedAt)
                    : query.OrderByDescending(j => j.StartedAt);
                break;
            case JobFilterSortBy.CompletedAt:
                orderedQuery = filter.Sort == SortDirection.Ascending
                    ? query.OrderBy(j => j.CompletedAt)
                    : query.OrderByDescending(j => j.CompletedAt);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
        
        return orderedQuery;
    }

    public IAsyncEnumerable<Job> GetFilteredJobs(JobFilter filter)
    {
        return GetFilteredJobsInternal(filter).AsAsyncEnumerable();
    }

    public IAsyncEnumerable<Job> GetFilteredJobsPaged(JobFilter filter, int page, int pageSize)
    {
        return GetFilteredJobsInternal(filter)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .AsAsyncEnumerable();
    }

    public IAsyncEnumerable<Job> GetPendingJobs()
    {
        throw new NotImplementedException();
    }

    public IAsyncEnumerable<Job> GetPendingJobsPaged(int page)
    {
        throw new NotImplementedException();
    }
}