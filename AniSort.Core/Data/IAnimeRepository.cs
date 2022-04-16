using System.Threading.Tasks;
using AniSort.Core.Models;

namespace AniSort.Core.Data;

public interface IAnimeRepository : IRepository<AnimeInfo, int>
{
}
