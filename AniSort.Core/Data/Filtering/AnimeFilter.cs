namespace AniSort.Core.Data.Filtering;

public class AnimeFilter : FilterBase<AnimeSortBy, Anime>
{
    /// <inheritdoc />
    public override bool Matches(Anime entity)
    {
        throw new System.NotImplementedException();
    }
}

public enum AnimeSortBy
{
    EnglishTitle = 1,
    RomajiTitle = 2,
    KanjiTitle = 3,
    AiredAt = 4,
}