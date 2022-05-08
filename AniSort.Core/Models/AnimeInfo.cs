using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using AniSort.Core.Extensions;

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
    List<EpisodeInfo> Episodes) : IMergable<AnimeInfo, int>
{

    /// <summary>
    /// Merge other object with this
    /// Will prefer values in calling object over other object
    /// </summary>
    /// <param name="other">Other object to merge with</param>
    /// <returns></returns>
    public AnimeInfo MergeWith(AnimeInfo other)
    {
        var merged = this with
        {
            TotalEpisodes = TotalEpisodes != default ? TotalEpisodes : other.TotalEpisodes,
            HighestEpisodeNumber = HighestEpisodeNumber != default ? HighestEpisodeNumber : other.HighestEpisodeNumber,
            Year = Year != default ? Year : other.Year,
            Type = Type ?? other.Type,
            RomajiName = RomajiName ?? other.RomajiName,
            KanjiName = KanjiName ?? other.KanjiName,
            EnglishName = EnglishName ?? other.EnglishName,
            RelatedAnimeIdList = RelatedAnimeIdList ?? other.RelatedAnimeIdList,
            Categories = Categories ?? other.Categories,
            OtherName = OtherName ?? other.OtherName,
            SynonymNames = SynonymNames ?? other.SynonymNames,
            Episodes = Episodes.Merge<EpisodeInfo, int>(other.Episodes).ToList()
        };

        for (int idx = 0; idx < merged.Episodes.Count; idx++)
        {
            merged.Episodes[idx] = merged.Episodes[idx] with { Anime = merged };
        }

        return merged;
    }

    /// <inheritdoc />
    public int Key => Id;

    /// <inheritdoc />
    public bool IsMergeableWith(AnimeInfo other)
    {
        return Id == other.Id;
    }

    public class IdComparer : Comparer<AnimeInfo>
    {

        /// <inheritdoc />
        public override int Compare(AnimeInfo? x, AnimeInfo? y)
        {
            return x switch
            {
                null when y == null => 0,
                null => -1,
                _ => y == null ? 1 : x.Id.CompareTo(y.Id)
            };

        }
    }
}
