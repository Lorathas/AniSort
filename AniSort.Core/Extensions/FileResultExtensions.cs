using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.RegularExpressions;
using AniDbSharp.Data;
using AniSort.Core.Data;
using AniSort.Core.Models;
using FileInfo = AniSort.Core.Models.FileInfo;

namespace AniSort.Core.Extensions;

public static class FileResultExtensions
{
    private static readonly Regex ResolutionRegex = new Regex(@"(?<width>\d+)x(?<height>\d+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    
    public static AnimeInfo ToAnimeInfo(this FileResult result)
    {
        if (result.FileInfo.AnimeId == null)
        {
            throw new ArgumentNullException(nameof(result.FileInfo.AnimeId));
        }

        if (result.FileInfo.EpisodeId == null)
        {
            throw new ArgumentNullException(nameof(result.FileInfo.EpisodeId));
        }

        if (result.FileInfo.GroupId == null)
        {
            throw new ArgumentNullException(nameof(result.FileInfo.GroupId));
        }
        
        var anime = new AnimeInfo(
                result.FileInfo.AnimeId.Value,
                result.AnimeInfo.TotalEpisodes ?? 0,
                result.AnimeInfo.HighestEpisodeNumber ?? 0,
                !string.IsNullOrWhiteSpace(result.AnimeInfo.Year) ? int.Parse(result.AnimeInfo.Year) : 0,
                result.AnimeInfo.Type,
                !string.IsNullOrWhiteSpace(result.AnimeInfo.RelatedAnimeIdList) && !string.IsNullOrWhiteSpace(result.AnimeInfo.RelatedAnimeIdType)
                    ? result.AnimeInfo.RelatedAnimeIdList.Split(',')
                        .Select(int.Parse)
                        .Zip(result.AnimeInfo.RelatedAnimeIdType.Split(','))
                        .Select(pair => new RelatedAnimeInfo(pair.First, pair.Second))
                        .ToList()
                    : new List<RelatedAnimeInfo>(),
                result.AnimeInfo.CategoryList?.Split(',').ToList() ?? new List<string>(),
                result.AnimeInfo.RomajiName,
                result.AnimeInfo.KanjiName,
                result.AnimeInfo.EnglishName,
                result.AnimeInfo.OtherName,
                result.AnimeInfo.SynonymList?.Split(',').ToList() ?? new List<string>(),
                null
            );
        
        
        var episode = new EpisodeInfo(
            result.FileInfo.EpisodeId.Value,
            result.FileInfo.AnimeId.Value,
            anime,
            result.AnimeInfo.EpisodeNumber,
            result.AnimeInfo.EpisodeName,
            result.AnimeInfo.RomajiName,
            result.AnimeInfo.KanjiName,
            result.AnimeInfo.EpisodeRating,
            result.AnimeInfo.EpisodeVoteCount,
            null
        );

        var file = new FileInfo(
            result.FileInfo.FileId,
            result.FileInfo.AnimeId.Value,
            result.FileInfo.EpisodeId.Value,
            episode,
            result.FileInfo.GroupId.Value,
            result.FileInfo.OtherEpisodes,
            result.FileInfo.IsDeprecated == 1,
            result.FileInfo.State ?? 0,
            result.AnimeInfo.GroupName,
            result.AnimeInfo.GroupShortName,
            result.FileInfo.Ed2kHash,
            result.FileInfo.Md5Hash,
            result.FileInfo.Sha1Hash,
            result.FileInfo.Crc32Hash,
            result.FileInfo.VideoColorDepth,
            result.FileInfo.Quality,
            result.FileInfo.Source,
            !string.IsNullOrWhiteSpace(result.FileInfo.AudioCodecList) && !string.IsNullOrWhiteSpace(result.FileInfo.AudioBitrateList)
                ? result.FileInfo.AudioCodecList.Split(',')
                    .Zip(result.FileInfo.AudioBitrateList.Split(',').Select(int.Parse))
                    .Select(pair => new CodecInfo(pair.First, pair.Second))
                    .ToList()
                : new List<CodecInfo>(),
            !string.IsNullOrWhiteSpace(result.FileInfo.VideoCodec)
                ? new CodecInfo(result.FileInfo.VideoCodec, result.FileInfo.VideoBitrate ?? 0)
                : default,
            ParseVideoResolution(result.FileInfo.VideoResolution),
            result.FileInfo.FileType,
            result.FileInfo.DubLanguage,
            result.FileInfo.SubLanguage,
            result.FileInfo.LengthInSeconds,
            result.FileInfo.Description,
            result.FileInfo.AiredDate.HasValue
                ? DateTimeOffset.FromUnixTimeMilliseconds(result.FileInfo.AiredDate.Value).UtcDateTime
                : default,
            result.FileInfo.AniDbFilename
        );

        return anime with { Episodes = new List<EpisodeInfo>{ episode with { Files = new List<FileInfo> { file }} } };
    }

    public static VideoResolution ParseVideoResolution([NotNull] this string videoResolution)
    {
        var match = ResolutionRegex.Match(videoResolution);
        
        return match.Success ? new VideoResolution(int.Parse(match.Groups["width"].Value), int.Parse(match.Groups["height"].Value)) : default;
    }
}
