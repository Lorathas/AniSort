using System;
using System.ComponentModel.DataAnnotations;

namespace AniSort.Core.Data;

public class AudioCodec
{
    [Key]
    public Guid Id { get; set; }
    public int FileId { get; set; }
    public virtual EpisodeFile File { get; set; }
    public string Codec { get; set; }
    public int Bitrate { get; set; }
}
