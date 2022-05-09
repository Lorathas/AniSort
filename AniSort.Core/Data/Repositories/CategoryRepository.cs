using System;

namespace AniSort.Core.Data.Repositories;

public class CategoryRepository : RepositoryBase<Category, Guid, AniSortContext>, ICategoryRepository
{

    /// <inheritdoc />
    public CategoryRepository(AniSortContext context) : base(context)
    {
    }
}
