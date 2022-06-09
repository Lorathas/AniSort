using System.Threading.Tasks;
using AniDbSharp.Data;

namespace AniSort.Core.Data.Repositories;

public interface IAnimeRepository : IRepository<Anime, int>
{
    (Anime, Episode, EpisodeFile) MergeSert(FileResult result, bool insertRelational = true);
    Task<(Anime, Episode, EpisodeFile)> MergeSertAsync(FileResult result, bool insertRelational = true);
    (Anime, Episode, EpisodeFile) MergeSert(FileResult result, LocalFile localFile);
    Task<(Anime, Episode, EpisodeFile)> MergeSertAsync(FileResult result, LocalFile localFile);
}
