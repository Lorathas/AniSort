using AniSort.Core.Data.Repositories.EF;
using LiteDB;

namespace AniSort.Core.Data.Repositories.LiteDB;

public class AnimeRepository : LiteDBRepositoryBase<Anime>
{
    public const string CollectionName = "anime";
    
    public AnimeRepository(LiteDatabase database) : base(database)
    {
    }

    protected override string ColName => CollectionName;
    protected override ObjectId GetObjectId(Anime entity) => entity.Id;
}