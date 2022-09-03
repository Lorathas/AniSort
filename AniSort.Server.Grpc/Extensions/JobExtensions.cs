using AniSort.Core.Data.Filtering;

namespace AniSort.Server.Extensions;

public static class JobExtensions
{
    public static JobFilter ToJobFilter(this FilteredJobsRequest request)
    {
        return new JobFilter
        {
            Sort = request.HasSort
                ? (Core.Data.Filtering.SortDirection)request.Sort
                : Core.Data.Filtering.SortDirection.Descending,
            SortBy = request.HasSortBy
                ? (Core.Data.Filtering.JobFilterSortBy)request.SortBy
                : Core.Data.Filtering.JobFilterSortBy.QueuedAt,
            Name = request.Name,
            Status = request.HasStatus
                ? (Core.Data.JobStatus?)request.Status
                : null,
            Type = request.HasType
            ? (Core.Data.JobType?)request.Type
            : null,
            StartTime = request.StartTime?.ToDateTimeOffset(),
            EndTime = request.EndTime?.ToDateTimeOffset(),
        };
    }
}