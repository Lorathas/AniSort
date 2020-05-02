namespace AniDbSharp.Data
{
    /// <summary>
    /// Container POCO for File Anime Info
    /// </summary>
    public class FileAnimeInfo
    {
        public int? TotalEpisodes { get; set; }
        public int? HighestEpisodeNumber { get; set; }
        public string Year { get; set; }
        public string Type { get; set; }
        public string RelatedAnimeIdList { get; set; }
        public string RelatedAnimeIdType { get; set; }
        public string CategoryList { get; set; }

        public string RomajiName { get; set; }
        public string KanjiName { get; set; }
        public string EnglishName { get; set; }
        public string OtherName { get; set; }
        public string SynonymList { get; set; }
        
        public string EpisodeNumber { get; set; }
        public string EpisodeName { get; set; }
        public string EpisodeRomajiName { get; set; }
        public string EpisodeKanjiName { get; set; }
        public int? EpisodeRating { get; set; }
        public int? EpisodeVoteCount { get; set; }

        public string GroupName { get; set; }
        public string GroupShortName { get; set; }
        public int? DateAnimeIdRecordUpdated { get; set; }
    }
}
