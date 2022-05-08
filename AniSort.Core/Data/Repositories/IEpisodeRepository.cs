using System.Threading.Tasks;
using AniSort.Core.Models;

namespace AniSort.Core.Data.Repositories;

public interface IEpisodeRepository : IRepository<EpisodeInfo, int>
{
    public EpisodeInfo GetByAnime(int id, int animeId);
    public Task<EpisodeInfo> GetByAnimeAsync(int id, int animeId);
}
