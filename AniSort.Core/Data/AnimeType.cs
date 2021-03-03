namespace AniSort.Core.Data
{
    /// <summary>
    /// Anime type enum based on values returned from AniDb
    /// </summary>
    public enum AnimeType
    {
        Sequel = 1,
        Prequel = 2,
        SameSetting = 11,
        AlternativeSetting = 12,
        AlternativeVersion = 32,
        MusicVideo = 41,
        Character = 42,
        SideStory = 51,
        ParentStory = 52,
        Summary = 61,
        FullStory = 62,
        Other = 100
    }
}