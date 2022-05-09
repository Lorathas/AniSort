using System.Threading.Tasks;
using AniDbSharp.Data;

namespace AniSort.Core.Data.Repositories;

public interface IAnimeRepository : IRepository<Anime, int>
{
    Anime MergeSert(FileResult result);
    Task<Anime> MergeSertAsync(FileResult result);
    Anime MergeSert(FileResult result, LocalFile localFile);
    Task<Anime> MergeSertAsync(FileResult result, LocalFile localFile);
}
