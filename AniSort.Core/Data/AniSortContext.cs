using Microsoft.EntityFrameworkCore;

namespace AniSort.Core.Data
{
    public class AniSortContext : DbContext
    {
        /// <inheritdoc />
        public AniSortContext(DbContextOptions options) : base(options)
        {
        }

        public DbSet<File> Files { get; set; }
        public DbSet<AnimeTitle> AnimeTitles { get; set; }
    }
}
