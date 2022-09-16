using System;
using System.Collections.Generic;

namespace AniSort.Core.Data.Repositories;

public interface IScheduledJobRepository : IRepository<ScheduledJob, Guid>
{
    /// <summary>
    /// Get all scheduled jobs ordered by name
    /// </summary>
    /// <returns></returns>
    IAsyncEnumerable<ScheduledJob> GetAllOrderedByName();

    /// <summary>
    /// Get scheduled jobs to work with the queue including their existing jobs, detached from the context
    /// </summary>
    /// <returns></returns>
    IAsyncEnumerable<ScheduledJob> GetForQueue();
}
