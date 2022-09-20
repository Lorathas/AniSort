using System;

namespace AniSort.Core.Data;

public class AnimeCategory : IEntity
{
    public int AnimeId { get; set; }
    public virtual Anime Anime { get; set; }
    public Guid CategoryId { get; set; }
    public virtual Category Category { get; set; }

    /// <inheritdoc />
    public bool IsNew => AnimeId != 0 && CategoryId != Guid.Empty;
}
