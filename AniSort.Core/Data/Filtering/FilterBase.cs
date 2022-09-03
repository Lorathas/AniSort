using System;

namespace AniSort.Core.Data.Filtering;

public abstract class FilterBase<TFields> where TFields : Enum
{
    public TFields SortBy { get; init; }
    public SortDirection Sort { get; init; }
}