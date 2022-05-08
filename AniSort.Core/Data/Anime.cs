using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using AniSort.Core.Models;

namespace AniSort.Core.Data;

public class Anime
{
    [Key]
    public int Id { get; set; }
    public int TotalEpisodes { get; set; }
    public int HighestEpisodeNumber { get; set; }
    public int Year { get; set; }
    public string Type { get; set; }
    public virtual ICollection<RelatedAnime> ChildrenAnime { get; set; }
    public virtual ICollection<RelatedAnime> ParentAnime { get; set; }
    public virtual ICollection<AnimeCategory> Categories { get; set; }
    public string RomajiName { get; set; }
    public string KanjiName { get; set; }
    public string EnglishName { get; set; }
    public string? OtherName { get; set; }
    public virtual ICollection<Synonym> Synonyms { get; set; }
    public virtual ICollection<Episode> Episodes { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
