using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AniSort.Core.Data;

public class AniSortContextFactory: IDesignTimeDbContextFactory<AniSortContext>
{

    /// <inheritdoc />
    public AniSortContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AniSortContext>();
        optionsBuilder.UseSqlite($"Data Source={AppPaths.DatabasePath}");

        return new AniSortContext(optionsBuilder.Options);
    }
}
