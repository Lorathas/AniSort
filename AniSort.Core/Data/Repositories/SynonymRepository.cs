using System;

namespace AniSort.Core.Data.Repositories;

public class SynonymRepository : RepositoryBase<Synonym, Guid, AniSortContext>, ISynonymRepository
{

    /// <inheritdoc />
    public SynonymRepository(AniSortContext context) : base(context)
    {
    }
}
