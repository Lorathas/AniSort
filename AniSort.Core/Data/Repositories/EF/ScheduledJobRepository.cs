using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace AniSort.Core.Data.Repositories.EF;

public class ScheduledJobRepository : RepositoryBase<ScheduledJob, Guid, AniSortContext>, IScheduledJobRepository
{
    /// <inheritdoc />
    public ScheduledJobRepository(AniSortContext context) : base(context)
    {
    }

    /// <inheritdoc />
    public IAsyncEnumerable<ScheduledJob> GetAllOrderedByName()
    {
        return Set
            .Where(j => !j.Deleted)
            .OrderBy(j => j.Name)
            .AsAsyncEnumerable();
    }

    /// <inheritdoc />
    public IAsyncEnumerable<ScheduledJob> GetForQueue()
    {
        return Set
            .Where(j => !j.Deleted)
            .OrderBy(j => j.Name)
            .Include(j => j.Jobs)
            .AsNoTracking()
            .AsAsyncEnumerable();
    }
}
