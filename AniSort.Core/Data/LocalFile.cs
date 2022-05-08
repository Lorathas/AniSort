using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace AniSort.Core.Data;

public class LocalFile
{
    [Key]
    public Guid Id { get; set; }
    public int EpisodeFileId { get; set; }
    public virtual EpisodeFile EpisodeFile { get; set; }
    public string Path { get; set; }
    // ReSharper disable once InconsistentNaming
    public byte[] Ed2kHash { get; set; }
    public virtual ICollection<FileAction> FileActions { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.Now;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.Now;
}
