using System.Threading.Tasks;
using AniSort.Core.Models;

namespace AniSort.Core.Data;

public interface IFileRepository : IRepository<FileInfo, int>
{
    FileInfo GetByEpisode(int id, int animeId, int episodeId);
    Task<FileInfo> GetByEpisodeAsync(int id, int animeId, int episodeId);
}
