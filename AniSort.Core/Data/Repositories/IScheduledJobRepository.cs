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
}
