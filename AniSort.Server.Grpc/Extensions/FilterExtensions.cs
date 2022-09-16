using AniSort.Core.Data.Filtering;

namespace AniSort.Server.Extensions;

public static class FilterExtensions
{
    public static JobFilter ToFilter(this FilteredJobsRequest request)
    {
        return new JobFilter
        {
            Page = request.Page,
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

    public static LocalFileFilter ToFilter(this FilteredLocalFilesRequest request)
    {
        return new LocalFileFilter
        {
            Page = request.Page,
            Search = request.HasSearch
                ? request.Search
                : null,
            Status = request.HasStatus
                ? (Core.Models.ImportStatus?)request.Status
                : null,
            StartTime = request.StartTime?.ToDateTimeOffset(),
            EndTime = request.EndTime?.ToDateTimeOffset(),
            Sort = request.HasSort
                ? (Core.Data.Filtering.SortDirection)request.Sort
                : Core.Data.Filtering.SortDirection.Descending,
            SortBy = request.HasSortBy
                ? (Core.Data.Filtering.LocalFileSortBy)request.SortBy
                : Core.Data.Filtering.LocalFileSortBy.CreatedAt,
        };
    }
}