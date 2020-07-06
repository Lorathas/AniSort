using System;
using System.Collections.Generic;
using System.Text;

namespace AniSort.Core.Data
{
    public interface ChangeTracking
    {
        DateTimeOffset CreatedAt { get; set; }
        DateTimeOffset UpdatedAt { get; set; }
    }
}
