using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using AniSort.Core.Models;
using LiteDB;

namespace AniSort.Core.Data;

public class Anime : IEntity
{
    public ObjectId Id { get; set; }
    public int AniDbId { get; set; }
    public int TotalEpisodes { get; set; }
    public int HighestEpisodeNumber { get; set; }
    public int Year { get; set; }
    public string Type { get; set; }
    public virtual List<RelatedAnime> ChildrenAnime { get; set; } = new();
    public virtual List<RelatedAnime> ParentAnime { get; set; } = new();
    public virtual List<AnimeCategory> Categories { get; set; } = new();
    public string? RomajiName { get; set; }
    public string? KanjiName { get; set; }
    public string? EnglishName { get; set; }
    public string? OtherName { get; set; }
    public virtual List<Synonym> Synonyms { get; set; } = new();
    public virtual List<Episode> Episodes { get; set; } = new();
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.Now;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.Now;

    /// <inheritdoc />
    public override string ToString()
    {
        return $"{AniDbId} {RomajiName ?? EnglishName ?? KanjiName ?? OtherName}";
    }

    /// <inheritdoc />
    public bool IsNew => AniDbId != 0;
}