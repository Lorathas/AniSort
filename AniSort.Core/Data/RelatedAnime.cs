using System;
using System.ComponentModel.DataAnnotations;
using AniSort.Core.Data.Repositories.LiteDB;
using LiteDB;

namespace AniSort.Core.Data;

public class RelatedAnime : IEntity
{
    public int DestinationAnimeId { get; set; }

    [BsonRef(AnimeRepository.CollectionName)]
    public virtual Anime DestinationAnime { get; set; }

    public string Relation { get; set; }

    /// <inheritdoc />
    public bool IsNew => false;
}