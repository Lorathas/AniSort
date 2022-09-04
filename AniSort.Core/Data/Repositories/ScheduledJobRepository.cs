using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace AniSort.Core.Data.Repositories;

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
            .OrderBy(j => j.Name)
            .AsAsyncEnumerable();
    }
}
