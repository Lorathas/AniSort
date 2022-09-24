namespace AniSort.Sdk.Data;

public record UserAnime<TId>(TId Id, string EnglishName, string RomajiName, string JapaneseName, int episodesCompleted = 0, int? AniDbId = null);
