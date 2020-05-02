namespace AniDbSharp.Data
{
    /// <summary>
    /// Container POCO for File Info
    /// </summary>
    public class FileInfo
    {
        public int FileId { get; set; }
        public int? AnimeId { get; set; }
        public int? EpisodeId { get; set; }
        public int? GroupId { get; set; }
        public int? MyListId { get; set; }
        public string OtherEpisodes { get; set; }
        public short? IsDeprecated { get; set; }
        public short? State { get; set; }

        public long? Size { get; set; }
        public byte[] Ed2kHash { get; set; }
        public byte[] Md5Hash { get; set; }
        public byte[] Sha1Hash { get; set; }
        public byte[] Crc32Hash { get; set; }
        public string VideoColorDepth { get; set; }

        public string Quality { get; set; }
        public string Source { get; set; }
        public string AudioCodecList { get; set; }
        public int? AudioBitrateList { get; set; }
        public string VideoCodec { get; set; }
        public int? VideoBitrate { get; set; }
        public string VideoResolution { get; set; }
        public string FileType { get; set; }

        public string DubLanguage { get; set; }
        public string SubLanguage { get; set; }
        public int? LengthInSeconds { get; set; }
        public string Description { get; set; }
        public int? AiredDate { get; set; }
        public string AniDbFilename { get; set; }

        public int? MyListState { get; set; }
        public int? MyListFileState { get; set; }
        public int? MyListViewed { get; set; }
        public int? MyListViewDate { get; set; }
        public int? MyListStorage { get; set; }
        public int? MyListSource { get; set; }
        public int? MyListOther { get; set; }
    }
}
