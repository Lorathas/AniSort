namespace AniSort.Core.Data.Repositories.EF;

public class EpisodeRepository : RepositoryBase<Episode, int, AniSortContext>, IEpisodeRepository
{

    /// <inheritdoc />
    public EpisodeRepository(AniSortContext context) : base(context)
    {
    }
}
