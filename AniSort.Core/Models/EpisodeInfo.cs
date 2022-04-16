using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace AniSort.Core.Models;

public record EpisodeInfo(
    int Id,
    int AnimeId,
    [property: JsonIgnore]
    AnimeInfo Anime,
    string Number,
    string EnglishName,
    string RomajiName,
    string KanjiName,
    int? Rating,
    int? VoteCount,
    List<FileInfo> Files);
