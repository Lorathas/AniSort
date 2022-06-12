using System.Threading.Tasks;

namespace AniSort.Core.Data.Repositories;

public interface IReleaseGroupRepository : IRepository<ReleaseGroup, int>
{
    bool ExistsForName(string name);
    Task<bool> ExistsForNameAsync(string name);
    bool ExistsForShortName(string shortName);
    Task<bool> ExistsForShortNameAsync(string shortName);
}
