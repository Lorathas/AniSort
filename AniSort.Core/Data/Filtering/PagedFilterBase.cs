using System;

namespace AniSort.Core.Data.Filtering;

public abstract class PagedFilterBase<TSortBy, TEntity> : FilterBase<TSortBy, TEntity> where TSortBy : Enum
{
    public int Page { get; set; }
}