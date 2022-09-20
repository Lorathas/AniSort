using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using AniSort.Core.Models;

namespace AniSort.Core.Data;

public class LocalFile : IEntity
{
    [Key]
    public Guid Id { get; set; }
    public int? EpisodeFileId { get; set; }
    public virtual EpisodeFile? EpisodeFile { get; set; }
    public string Path { get; set; }
    // ReSharper disable once InconsistentNaming
    public byte[]? Ed2kHash { get; set; }
    public long FileLength { get; set; }
    public virtual ICollection<FileAction> FileActions { get; set; } = new List<FileAction>();
    public ImportStatus Status { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.Now;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.Now;
    public virtual ICollection<Job> Jobs { get; set; } = new List<Job>();

    /// <inheritdoc />
    public bool IsNew => Id != Guid.Empty;
}
