using System;
using System.Collections.Generic;
using System.Linq;

namespace AniSort.Core.Data.Repositories;

public interface IFileActionRepository : IRepository<FileAction, Guid>
{
    IEnumerable<FileAction> GetForFile(Guid fileId);
    IAsyncEnumerable<FileAction> GetForFileAsync(Guid fileId);
}
