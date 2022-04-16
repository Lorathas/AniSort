using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace AniSort.Core.Models;

public record AnimeInfo(
    int Id,
    int TotalEpisodes,
    int HighestEpisodeNumber,
    int Year,
    string Type,
    List<RelatedAnime> RelatedAnimeIdList,
    List<string> Categories,
    string RomajiName,
    string KanjiName,
    string EnglishName,
    string? OtherName,
    List<string> SynonymNames,
    List<EpisodeInfo> Episodes);
