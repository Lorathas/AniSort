using System;

namespace AniSort.Core.Data.Filtering;

public abstract class FilterBase<TFields, TEntity> where TFields : Enum
{
    public TFields SortBy { get; init; }
    public SortDirection Sort { get; init; }

    public abstract bool Matches(TEntity entity);
}