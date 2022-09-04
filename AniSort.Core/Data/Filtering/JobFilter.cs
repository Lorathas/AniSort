using System;

namespace AniSort.Core.Data.Filtering;

public class JobFilter : PagedFilterBase<JobFilterSortBy>
{
    public JobStatus? Status { get; init; }
    public string Name { get; init; }
    public JobType? Type { get; init; }
    public DateTimeOffset? StartTime { get; init; }
    public DateTimeOffset? EndTime { get; init; }
}

public enum JobFilterSortBy
{
    Id = 0,
    Name = 1,
    Type = 2,
    Status = 3,
    PercentComplete = 4,
    IsFinished = 5,
    QueuedAt = 6,
    StartedAt = 7,
    CompletedAt = 8,
}