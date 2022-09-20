using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace AniSort.Core.Data;

public class AniSortContextFactory : IDesignTimeDbContextFactory<AniSortContext>
{
    /// <inheritdoc />
    public AniSortContext CreateDbContext(string[] args)
    {
        IConfigurationRoot configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json")
            .Build();

        var builder = new DbContextOptionsBuilder<AniSortContext>();
        var connectionString = configuration.GetConnectionString("Postgres");

        builder.UseNpgsql(connectionString);

        return new AniSortContext(builder.Options);
    }
}
