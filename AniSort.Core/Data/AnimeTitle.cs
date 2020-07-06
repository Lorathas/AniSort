using System;
using System.ComponentModel.DataAnnotations;

namespace AniSort.Core.Data
{
    public class AnimeTitle
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();
        public int AnimeId { get; set; }
        public TitleType Type { get; set; }
        public string Language { get; set; }
    }
}
