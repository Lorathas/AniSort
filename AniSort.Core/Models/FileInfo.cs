using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using AniDbSharp.Data;

namespace AniSort.Core.Models;

public record FileInfo(
    int Id,
    int AnimeId,
    int EpisodeId,
    [property: JsonIgnore] EpisodeInfo Episode,
    int GroupId,
    string OtherEpisodes,
    bool IsDeprecated,
    FileState State,
    string GroupName,
    string GroupShortName,
    // ReSharper disable once InconsistentNaming
    byte[] Ed2kHash,
    byte[] Md5Hash,
    byte[] Sha1Hash,
    byte[] Crc32Hash,
    string VideoColorDepth,
    string Quality,
    string Source,
    List<CodecInfo> AudioCodecs,
    CodecInfo VideoCodec,
    VideoResolution VideoResolution,
    string FileType,
    string DubLanguage,
    string SubLanguage,
    int? LengthInSeconds,
    string Description,
    DateTime AiredDate,
    string AniDbFilename) : IMergable<FileInfo, int>
{
    public class IdComparer : Comparer<FileInfo>
    {

        /// <inheritdoc />
        public override int Compare(FileInfo? x, FileInfo? y)
        {
            return x switch
            {
                null when y == null => 0,
                null => -1,
                _ => y == null ? 1 : x.Id.CompareTo(y.Id)
            };
        }
    }

    /// <inheritdoc />
    public FileInfo MergeWith(FileInfo other)
    {
        return this with
        {
            AnimeId = AnimeId != default ? AnimeId : other.AnimeId,
            EpisodeId = EpisodeId != default ? EpisodeId : other.EpisodeId,
            Episode = Episode ?? other.Episode,
            GroupId = GroupId != default ? GroupId : other.GroupId,
            OtherEpisodes = OtherEpisodes ?? other.OtherEpisodes,
            IsDeprecated = IsDeprecated != default ? IsDeprecated : other.IsDeprecated,
            State = State != default ? State : other.State,
            GroupName = GroupName ?? other.GroupName,
            GroupShortName = GroupShortName ?? other.GroupShortName,
            Ed2kHash = Ed2kHash ?? other.Ed2kHash,
            Md5Hash = Md5Hash ?? other.Md5Hash,
            Sha1Hash = Sha1Hash ?? other.Sha1Hash,
            Crc32Hash = Crc32Hash ?? other.Crc32Hash,
            VideoColorDepth = VideoColorDepth ?? other.VideoColorDepth,
            Quality = Quality ?? other.Quality,
            Source = Source ?? other.Source,
            AudioCodecs = AudioCodecs ?? other.AudioCodecs,
            VideoCodec = VideoCodec ?? other.VideoCodec,
            VideoResolution = VideoResolution ?? other.VideoResolution,
            FileType = FileType ?? other.FileType,
            DubLanguage = DubLanguage ?? other.DubLanguage,
            SubLanguage = SubLanguage ?? other.SubLanguage,
            LengthInSeconds = LengthInSeconds ?? other.LengthInSeconds,
            Description = Description ?? other.Description,
            AiredDate = AiredDate != default ? AiredDate : other.AiredDate,
            AniDbFilename = AniDbFilename ?? other.AniDbFilename
        };
    }

    /// <inheritdoc />
    public int Key => Id;

    /// <inheritdoc />
    public bool IsMergeableWith(FileInfo other)
    {
        return Id == other.Id;
    }
}
