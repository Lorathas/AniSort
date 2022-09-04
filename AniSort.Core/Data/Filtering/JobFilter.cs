using System;
using System.Linq;

namespace AniSort.Core.Data.Filtering;

public class JobFilter : PagedFilterBase<JobFilterSortBy, Job>
{
    public JobStatus? Status { get; init; }

    public string Name { get; init; }

    public JobType? Type { get; init; }

    public DateTimeOffset? StartTime { get; init; }

    public DateTimeOffset? EndTime { get; init; }

    /// <inheritdoc />
    public override bool Matches(Job entity)
    {
        return (Status == null || entity.Status == Status)
               && (string.IsNullOrWhiteSpace(Name) || Name.Split(' ', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).All(w => entity.Name.ToLowerInvariant().Contains(w)))
               && (Type == null || entity.Status == Status)
               && (StartTime == null || entity.QueuedAt >= StartTime || entity.StartedAt >= StartTime || entity.CompletedAt >= StartTime)
               && (EndTime == null || entity.QueuedAt < EndTime || entity.StartedAt < EndTime || entity.CompletedAt < EndTime);
    }
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
