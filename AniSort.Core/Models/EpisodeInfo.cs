using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using AniSort.Core.Extensions;

namespace AniSort.Core.Models;

public record EpisodeInfo(
    int Id,
    int AnimeId,
    [property: JsonIgnore] AnimeInfo Anime,
    string Number,
    string EnglishName,
    string RomajiName,
    string KanjiName,
    int? Rating,
    int? VoteCount,
    List<FileInfo> Files) : IMergable<EpisodeInfo, int>
{

    public EpisodeNumber ComplexEpisodeNumber
    {
        get
        {
            EpisodeType type = EpisodeType.Normal;
            int number;

            if (Number.StartsWith("S"))
            {
                type = EpisodeType.Special;
                number = int.Parse(Number[1..]);
            }
            else if (Number.StartsWith("OP") || Number.StartsWith("ED"))
            {
                type = EpisodeType.OpeningClosing;
                number = int.Parse(Number[2..]);
            }
            else if (Number.StartsWith("C"))
            {
                type = EpisodeType.OpeningClosing;
                number = int.Parse(Number[1..]);
            }
            else if (Number.StartsWith("T"))
            {
                type = EpisodeType.Trailer;
                number = int.Parse(Number[1..]);
            }
            else if (Number.StartsWith("P"))
            {
                type = EpisodeType.Parody;
                number = int.Parse(Number[1..]);
            }
            else if (Number.StartsWith("O"))
            {
                type = EpisodeType.Other;
                number = int.Parse(Number[1..]);
            }
            else
            {
                number = int.Parse(Number);
            }

            return new EpisodeNumber(type, number);
        }
    }

    #region Comparers

    public class IdComparer : Comparer<EpisodeInfo>
    {
        /// <inheritdoc />
        public override int Compare(EpisodeInfo? x, EpisodeInfo? y)
        {
            return x switch
            {
                null when y == null => 0,
                null => -1,
                _ => y == null ? 1 : x.Id.CompareTo(y.Id)
            };
        }
    }

    public class NumberComparer : Comparer<EpisodeInfo>
    {
        /// <inheritdoc />
        public override int Compare(EpisodeInfo? x, EpisodeInfo? y)
        {
            return x switch
            {
                null when y == null => 0,
                null => -1,
                _ => y == null ? 1 : x.ComplexEpisodeNumber.CompareTo(y.ComplexEpisodeNumber)
            };
        }
    }

    #endregion

    /// <inheritdoc />
    public EpisodeInfo MergeWith(EpisodeInfo other)
    {
        var merged = this with
        {
            AnimeId = AnimeId != default ? AnimeId : other.AnimeId,
            Anime = Anime ?? other.Anime,
            Number = Number ?? other.Number,
            RomajiName = RomajiName ?? other.RomajiName,
            KanjiName = KanjiName ?? other.KanjiName,
            EnglishName = EnglishName ?? other.EnglishName,
            Rating = Rating ?? other.Rating,
            VoteCount = VoteCount ?? other.VoteCount,
            Files = Files.Merge<FileInfo, int>(other.Files).ToList()
        };

        for (int idx = 0; idx < merged.Files.Count; idx++)
        {
            merged.Files[idx] = merged.Files[idx] with { Episode = merged };
        }

        return merged;
    }

    /// <inheritdoc />
    public bool IsMergeableWith(EpisodeInfo other)
    {
        return Id == other.Id;
    }

    public int Key => Id;
}
