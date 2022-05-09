using System;
using System.ComponentModel.DataAnnotations;

namespace AniSort.Core.Data;

public class Synonym
{
    [Key]
    public Guid Id { get; set; }
    public string Value { get; set; }
    public int AnimeId { get; set; }
    public virtual Anime Anime { get; set; }
}
