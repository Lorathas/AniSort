namespace AniSort.Core.Data.Repositories;

public class EpisodeRepository : RepositoryBase<Episode, int, AniSortContext>, IEpisodeRepository
{

    /// <inheritdoc />
    public EpisodeRepository(AniSortContext context) : base(context)
    {
    }
}
