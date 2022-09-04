using System;
using System.Linq;
using AniSort.Core.Models;

namespace AniSort.Core.Data.Filtering;

public class LocalFileFilter : PagedFilterBase<LocalFileSortBy, LocalFile>
{
    public string Search { get; set; }
    public DateTimeOffset? StartTime { get; set; }
    public DateTimeOffset? EndTime { get; set; }
    public ImportStatus? Status { get; set; }

    /// <inheritdoc />
    public override bool Matches(LocalFile entity)
    {
        return (string.IsNullOrWhiteSpace(Search) || Search.Split(' ', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).All(w => entity.Path.ToLowerInvariant().Contains(w)))
               && (StartTime == null || entity.CreatedAt >= StartTime || entity.UpdatedAt >= StartTime)
               && (EndTime == null || entity.CreatedAt < EndTime || entity.UpdatedAt < EndTime)
               && (Status == null || entity.Status == Status);
    }
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