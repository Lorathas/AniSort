using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using AniDbSharp.Data;

namespace AniSort.Core.Models;

public record FileInfo(
    int Id,
    int AnimeId,
    int EpisodeId,
    [property: JsonIgnore]
    EpisodeInfo Episode,
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
    int LengthInSeconds,
    string Description,
    DateTime AiredDate,
    string AniDbFilename);
