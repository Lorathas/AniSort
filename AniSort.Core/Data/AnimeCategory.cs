using System;

namespace AniSort.Core.Data;

public class AnimeCategory
{
    public int AnimeId { get; set; }
    public virtual Anime Anime { get; set; }
    public Guid CategoryId { get; set; }
    public virtual Category Category { get; set; }
}
