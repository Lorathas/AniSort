using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
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
    /// <param name="pageSize">Size of the pages</param>
    /// <returns></returns>
    IAsyncEnumerable<Job> GetPendingJobsPaged(int page, int pageSize);

    /// <summary>
    /// Get the most recent job for a scheduled job
    /// </summary>
    /// <param name="scheduledJobId">Id of the scheduled job</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns></returns>
    Task<Job> GetLastJobForScheduledJobAsync(Guid scheduledJobId, CancellationToken? cancellationToken = null);
}