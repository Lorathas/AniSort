// Copyright © 2022 Lorathas
//
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation
// files (the “Software”), to deal in the Software without restriction, including without limitation the rights to use, copy,
// modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the
// Software is furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED “AS IS”, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
// OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR
// IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

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
