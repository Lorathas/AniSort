using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using AniDbSharp.Data;
using AniSort.Core.Models;

namespace AniSort.Core.Data;

public class EpisodeFile
{
    [Key]
    public int Id { get; set; }

    public int EpisodeId { get; set; }
    public virtual Episode Episode { get; set; }
    public int GroupId { get; set; }
    public virtual ReleaseGroup Group { get; set; }
    public string OtherEpisodes { get; set; }
    public bool IsDeprecated { get; set; }
    public FileState State { get; set; }
    public byte[] Ed2kHash { get; set; }
    public byte[] Md5Hash { get; set; }
    public byte[] Sha1Hash { get; set; }
    public byte[] Crc32Hash { get; set; }
    public string VideoColorDepth { get; set; }
    public string Quality { get; set; }
    public string Source { get; set; }
    public virtual ICollection<AudioCodec> AudioCodecs { get; set; } = new List<AudioCodec>();
    public string VideoCodec { get; set; }
    public int VideoBitrate { get; set; }
    public int VideoWidth { get; set; }
    public int VideoHeight { get; set; }
    public string FileType { get; set; }
    public string DubLanguage { get; set; }
    public string SubLanguage { get; set; }
    public int? LengthInSeconds { get; set; }
    public string Description { get; set; }
    public DateTime AiredDate { get; set; }
    public string AniDbFilename { get; set; }

    public virtual ICollection<LocalFile> LocalFiles { get; set; } = new List<LocalFile>();

    public VideoResolution Resolution
    {
        get => new(VideoWidth, VideoHeight);
        set
        {
            VideoWidth = value.Width;
            VideoHeight = value.Height;
        }
    }
}
