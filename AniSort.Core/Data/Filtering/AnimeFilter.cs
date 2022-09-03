namespace AniSort.Core.Data.Filtering;

public class AnimeFilter : FilterBase<AnimeSortBy>
{
    
}

public enum AnimeSortBy
{
    EnglishTitle = 1,
    RomajiTitle = 2,
    KanjiTitle = 3,
    AiredAt = 4,
}