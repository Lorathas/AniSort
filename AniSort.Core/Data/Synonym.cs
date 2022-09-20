using System;
using System.ComponentModel.DataAnnotations;

namespace AniSort.Core.Data;

public class Synonym : IEntity
{
    [Key]
    public Guid Id { get; set; }
    public string Value { get; set; }
    public int AnimeId { get; set; }
    public virtual Anime Anime { get; set; }

    /// <inheritdoc />
    public bool IsNew => Id != Guid.Empty;
}
