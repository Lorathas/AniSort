using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace AniSort.Core.Data.Repositories;

public class ReleaseGroupRepository : RepositoryBase<ReleaseGroup, int, AniSortContext>, IReleaseGroupRepository
{

    /// <inheritdoc />
    public ReleaseGroupRepository(AniSortContext context) : base(context)
    {
    }

    /// <inheritdoc />
    public bool ExistsForName(string name) => Set.Any(g => g.Name == name);

    /// <inheritdoc />
    public async Task<bool> ExistsForNameAsync(string name) => await Set.AnyAsync(g => g.Name == name);

    /// <inheritdoc />
    public bool ExistsForShortName(string shortName) => Set.Any(g => g.ShortName == shortName);

    /// <inheritdoc />
    public async Task<bool> ExistsForShortNameAsync(string shortName) => await Set.AnyAsync(g => g.ShortName == shortName);
}
