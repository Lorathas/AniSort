using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace AniSort.Core.Data
{
    [Index("Name", IsUnique = true)]
    public class Tag
    {
        [Key]
        public int Id { get; set; }
        public string Name { get; set; }
        
        public ICollection<Anime> Anime { get; set; }
    }
}
