namespace AniSort.Core.Data.Repositories;

public class ReleaseGroupRepository : RepositoryBase<ReleaseGroup, int, AniSortContext>, IReleaseGroupRepository
{

    /// <inheritdoc />
    public ReleaseGroupRepository(AniSortContext context) : base(context)
    {
    }
}
