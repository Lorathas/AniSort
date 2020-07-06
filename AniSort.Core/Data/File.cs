using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
using Microsoft.EntityFrameworkCore;

namespace AniSort.Core.Data
{
    public class File : ChangeTracking
    {
        [Key]
        public Guid Id { get; set; }
        [Required]
        public string Path { get; set; }
        public SortStatus SortStatus { get; set; }
        public virtual ICollection<FileLog> Logs { get; set; } = new List<FileLog>();
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.Now;
        public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.Now;
    }
}
