using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace AniSort.Core.Data
{
    public class Anime
    {
        [Key]
        public int AniDbId { get; set; }
        public int? StartYear { get; set; }
        public int? EndYear { get; set; }
        public AnimeType Type { get; set; }
        public string RomajiName { get; set; }
        public string EnglishName { get; set; }
        public string KanjiName { get; set; }
        public string OtherName { get; set; }
        public int EpisodeCount { get; set; }
        public int SpecialEpisodeCount { get; set; }
        public int HighestEpisodeNumber { get; set; }

        public ICollection<Tag> Tags { get; set; }
    }
}