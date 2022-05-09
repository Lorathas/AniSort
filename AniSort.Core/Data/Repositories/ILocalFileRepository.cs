using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AniSort.Core.Data.Repositories;

public interface ILocalFileRepository : IRepository<LocalFile, Guid>
{
    LocalFile GetForEd2kHash(byte[] hash);
    Task<LocalFile> GetForEd2kHashAsync(byte[] hash);
    bool ExistsForEd2kHash(byte[] hash);
    Task<bool> ExistsForEd2kHashAsync(byte[] hash);
    LocalFile GetForPath(string path);
    Task<LocalFile> GetForPathAsync(string path);
    bool ExistsForPath(string path);
    Task<bool> ExistsForPathAsync(string path);
    IEnumerable<LocalFile> GetWithoutResolution();
    IAsyncEnumerable<LocalFile> GetWithoutResolutionAsync();
}
