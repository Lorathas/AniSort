﻿using System;
using AniSort.Core.Models;

namespace AniSort.Core.Data.Filtering;

public class LocalFileFilter : FilterBase<LocalFileSortBy>
{
    public string Search { get; set; }
    public DateTimeOffset? Start { get; set; }
    public DateTimeOffset? End { get; set; }
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