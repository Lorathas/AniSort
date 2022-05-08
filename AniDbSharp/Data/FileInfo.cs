// Copyright © 2020 Lorathas
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
        public FileState? State { get; set; }

        public long? Size { get; set; }
        public byte[] Ed2kHash { get; set; }
        public byte[] Md5Hash { get; set; }
        public byte[] Sha1Hash { get; set; }
        public byte[] Crc32Hash { get; set; }
        public string VideoColorDepth { get; set; }

        public string Quality { get; set; }
        public string Source { get; set; }
        public string AudioCodecList { get; set; }
        public string AudioBitrateList { get; set; }
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

        public bool HasResolution => !string.IsNullOrWhiteSpace(VideoResolution) && VideoResolution != "0x0";
    }
}
