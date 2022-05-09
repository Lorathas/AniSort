using AniSort.Core.Data.Repositories;
using AniSort.Core.Models;

namespace AniSort.Core.Data.FileStore;

public interface IFileAnimeRepository : IRepository<AnimeInfo, int>
{
    /// <summary>
    /// Merge or Insert anime into repository
    /// </summary>
    /// <param name="animeInfo">Anime to merge or insert</param>
    public void MergeSert(AnimeInfo animeInfo);
}
