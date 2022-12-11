using AniSort.Core.Data.Repositories.EF;

namespace AniSort.Core.Data.Repositories;

public class EpisodeFileRepository : RepositoryBase<EpisodeFile, int, AniSortContext>, IEpisodeFileRepository
{

    /// <inheritdoc />
    public EpisodeFileRepository(AniSortContext context) : base(context)
    {
    }
}
