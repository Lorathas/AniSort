using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace AniSort.Core.Data.Repositories.EF;

public class FileActionRepository : RepositoryBase<FileAction, Guid, AniSortContext>, IFileActionRepository
{

    /// <inheritdoc />
    public FileActionRepository(AniSortContext context) : base(context)
    {
    }

    /// <inheritdoc />
    public IEnumerable<FileAction> GetForFile(Guid fileId)
    {
        return Set.Where(a => a.FileId == fileId);
    }

    /// <inheritdoc />
    public IAsyncEnumerable<FileAction> GetForFileAsync(Guid fileId)
    {
        return Set.Where(a => a.FileId == fileId)
            .AsAsyncEnumerable();
    }
}
