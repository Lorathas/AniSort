using System;

namespace AniSort.Core.Data.Repositories;

public class FileActionRepository : RepositoryBase<FileAction, Guid, AniSortContext>, IFileActionRepository
{

    /// <inheritdoc />
    public FileActionRepository(AniSortContext context) : base(context)
    {
    }
}
