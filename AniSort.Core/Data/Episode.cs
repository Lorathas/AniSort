using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using AniSort.Core.Models;

namespace AniSort.Core.Data;

public class Episode
{
    [Key]
    public int Id { get; set; }
    public int AnimeId { get; set; }
    public virtual Anime Anime { get; set; }
    public string Number { get; set; }
    public string EnglishName { get; set; }
    public string RomajiName { get; set; }
    public string KanjiName { get; set; }
    public int? Rating { get; set; }
    public int? VoteCount { get; set; }
    public virtual ICollection<EpisodeFile> Files { get; set; } = new List<EpisodeFile>();

    /// <inheritdoc />
    public override string ToString()
    {
        return $"{Number} {EnglishName ?? RomajiName ?? KanjiName}";
    }
}
