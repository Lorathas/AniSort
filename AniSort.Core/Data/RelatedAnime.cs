using System;
using System.ComponentModel.DataAnnotations;

namespace AniSort.Core.Data;

public class RelatedAnime : IEntity
{
    [Key]
    public Guid Id { get; set; }
    public int SourceAnimeId { get; set; }
    public virtual Anime SourceAnime { get; set; }
    public int DestinationAnimeId { get; set; }
    public virtual Anime DestinationAnime { get; set; }
    public string Relation { get; set; }

    /// <inheritdoc />
    public bool IsNew => Id != Guid.Empty;
}
