using System;

namespace AniSort.Core.Data.Repositories;

public class JobStepRepository : RepositoryBase<JobStep, Guid, AniSortContext>, IJobStepRepository
{
    /// <inheritdoc />
    public JobStepRepository(AniSortContext context) : base(context)
    {
    }
}
