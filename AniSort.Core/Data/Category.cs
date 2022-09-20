using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace AniSort.Core.Data;

public class Category : IEntity
{
    [Key]
    public Guid Id { get; set; }
    public string Value { get; set; }
    public virtual ICollection<AnimeCategory> Anime { get; set; } = new List<AnimeCategory>();

    /// <inheritdoc />
    public bool IsNew => Id != Guid.Empty;
}
