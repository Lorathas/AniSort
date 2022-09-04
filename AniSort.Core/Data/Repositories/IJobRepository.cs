using System;
using System.Collections.Generic;
using AniSort.Core.Data.Filtering;

namespace AniSort.Core.Data.Repositories;

public interface IJobRepository : IRepository<Job, Guid>
{
    /// <summary>
    /// Get filtered jobs
    /// </summary>
    /// <param name="filter">Filter and sort configuration</param>
    /// <returns></returns>
    IAsyncEnumerable<Job> GetFilteredJobs(JobFilter filter);

    /// <summary>
    /// Get a page of filtered jobs
    /// </summary>
    /// <param name="filter">Filter and sort configuration</param>
    /// <param name="page">Page to get</param>
    /// <param name="pageSize">Size of each page</param>
    /// <returns></returns>
    IAsyncEnumerable<Job> GetFilteredJobsPaged(JobFilter filter, int pageSize);

    /// <summary>
    /// Get pending jobs
    /// </summary>
    /// <returns></returns>
    IAsyncEnumerable<Job> GetPendingJobs();

    /// <summary>
    /// Get a page of pending jobs
    /// </summary>
    /// <param name="page">Page to get</param>
    /// <returns></returns>
    IAsyncEnumerable<Job> GetPendingJobsPaged(int page);
}