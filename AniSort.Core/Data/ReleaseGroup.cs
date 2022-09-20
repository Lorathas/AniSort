using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace AniSort.Core.Data;

public class ReleaseGroup : IEntity
{
    [Key]
    public int Id { get; set; }
    public string Name { get; set; }
    public string ShortName { get; set; }
    public virtual ICollection<EpisodeFile> Files { get; set; } = new List<EpisodeFile>();
    
    public const int UnknownId = 2147483647;

    /// <inheritdoc />
    public bool IsNew => Id != 0;
}
