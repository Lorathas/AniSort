using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace AniSort.Core.Data
{
    public class FileLog : ChangeTracking
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid FileId { get; set; }

        public virtual File File { get; set; }

        public string Message { get; set; }

        /// <inheritdoc />
        public DateTimeOffset CreatedAt { get; set; }

        /// <inheritdoc />
        public DateTimeOffset UpdatedAt { get; set; }
    }
}