using System;

namespace AniSort.Core.Data.Filtering;

public abstract class PagedFilterBase<TSortBy> : FilterBase<TSortBy> where TSortBy : Enum
{
    public int Page { get; set; }
}