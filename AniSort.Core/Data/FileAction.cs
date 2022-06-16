using System;
using System.ComponentModel.DataAnnotations;

namespace AniSort.Core.Data;

public class FileAction
{
    [Key]
    public Guid Id { get; set; }
    public FileActionType Type { get; set; }
    public bool Success { get; set; }
    public string Info { get; set; }
    public string Exception { get; set; }
    public Guid FileId { get; set; }
    public virtual LocalFile File { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.Now;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.Now;

    public bool IsCoolingDown(TimeSpan cooldownTime)
    {
        var now = DateTimeOffset.Now;

        return CreatedAt.Add(cooldownTime) >= now && UpdatedAt.Add(cooldownTime) >= now;
    }

    /// <inheritdoc />
    public override string ToString() => $"File {FileId} Action {Type}:{Success} at {CreatedAt}";
}
