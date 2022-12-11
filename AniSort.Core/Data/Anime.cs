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
    public virtual ICollection<RelatedAnime> ChildrenAnime { get; set; } = new List<RelatedAnime>();
    public virtual ICollection<RelatedAnime> ParentAnime { get; set; } = new List<RelatedAnime>();
    public virtual ICollection<AnimeCategory> Categories { get; set; } = new List<AnimeCategory>();
    public string? RomajiName { get; set; }
    public string? KanjiName { get; set; }
    public string? EnglishName { get; set; }
    public string? OtherName { get; set; }
    public virtual ICollection<Synonym> Synonyms { get; set; } = new List<Synonym>();
    public virtual ICollection<Episode> Episodes { get; set; } = new List<Episode>();
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.Now;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.Now;

    /// <inheritdoc />
    public override string ToString()
    {
        return $"{Id} {RomajiName ?? EnglishName ?? KanjiName ?? OtherName}";
    }
}
