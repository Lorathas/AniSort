using System;
using AniSort.Core.Models;

namespace AniSort.Core.Data.Filtering;

public class LocalFileFilter : PagedFilterBase<LocalFileSortBy>
{
    public string Search { get; set; }
    public DateTimeOffset? StartTime { get; set; }
    public DateTimeOffset? EndTime { get; set; }
    public ImportStatus? Status { get; set; }
}

public enum LocalFileSortBy
{
    Path = 0,
    Length = 1,
    Hash = 2,
    Status = 3,
    CreatedAt = 4,
    UpdatedAt = 5,
}