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

using Microsoft.VisualStudio.TestTools.UnitTesting;
using AniSort.Core.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using AniDbSharp.Data;
using AniDbSharp.Extensions;
using AniSort.Core.Utils;
using FileInfo = AniDbSharp.Data.FileInfo;

namespace AniSort.Core.IO.Tests
{
    [TestClass()]
    public class PathBuilderTests
    {
        [TestMethod()]
        public void CompileTest()
        {
            var fileInfo = new FileInfo
            {
                AiredDate = (int) (new DateTime(2004, 1, 1, 0, 0, 0, DateTimeKind.Utc) - DateTime.UnixEpoch)
                    .TotalSeconds,
                State = FileState.CrcOk | FileState.IsVersion2,
                AniDbFilename = "[Hi10] Koukaku Kidoutai S.A.C. 2nd GIG - 02v2 [D7083952].mkv",
                AnimeId = 1176,
                AudioBitrateList = "166",
                AudioCodecList = "Ogg (vorbis)",
                Crc32Hash = "D7083952".HexStringToBytes(),
                Description = "",
                DubLanguage = "english",
                Ed2kHash = "cc1aebd6596d44374529b0352f74fc7a".HexStringToBytes(),
                EpisodeId = 12831,
                FileId = 1265031,
                FileType = "video",
                GroupId = 11111,
                VideoResolution = "1280x688",
                VideoCodec = "h264"
            };
            var animeInfo = new FileAnimeInfo
            {
                CategoryList = "test'test",
                EpisodeName = "Night Cruise",
                EpisodeRomajiName = "Houshoku no Boku NIGHT CRUISE",
                EpisodeKanjiName = "飽食の僕 NIGHT CRUISE",
                EnglishName = "Ghost in the Shell: Stand Alone Complex 2nd GIG",
                RomajiName = "Koukaku Kidoutai S.A.C. 2nd GIG",
                KanjiName = "攻殻機動隊 S.A.C. 2nd GIG GHOST IN THE SHELL \"STAND ALONE COMPLEX\"",
                EpisodeNumber = "2",
                HighestEpisodeNumber = 26,
                TotalEpisodes = 26,
                GroupName = "Hi10 Anime",
                GroupShortName = "Hi10",
                Type = "TV Series",
            };

            var builder = PathBuilder.Compile("testRoot", "tv", "movie", $"{{animeEnglish}}{Path.DirectorySeparatorChar}test");

            Assert.IsTrue(builder.AnimeMask.SecondByteFlags.HasFlag(FileAnimeMaskSecondByte.EnglishName));
            Assert.IsTrue(builder.AnimeMask.FirstByteFlags.HasFlag(FileAnimeMaskFirstByte.Type));

            string path = builder.BuildPath(fileInfo, animeInfo, PlatformUtils.MaxPathLength);

            Assert.AreEqual($"testRoot{Path.DirectorySeparatorChar}tv{Path.DirectorySeparatorChar}Ghost in the Shell_ Stand Alone Complex 2nd GIG{Path.DirectorySeparatorChar}test", path, true);

            builder = PathBuilder.Compile("testRoot", "tv", "movie",
                $"{{animeRomaji}}{Path.DirectorySeparatorChar}{{animeRomaji}} - {{episodeNumber}}{{'v'fileVersion}} - {{episodeEnglish...}}[{{groupShort}}][{{resolution}}][{{videoCodec}}][{{crc32}}]");

            Assert.IsTrue(builder.FileMask.FirstByteFlags.HasFlag(FileMaskFirstByte.State));
            Assert.IsTrue(builder.FileMask.SecondByteFlags.HasFlag(FileMaskSecondByte.Crc32));
            Assert.IsTrue(builder.FileMask.ThirdByteFlags.HasFlag(FileMaskThirdByte.VideoResolution));
            Assert.IsTrue(builder.FileMask.ThirdByteFlags.HasFlag(FileMaskThirdByte.VideoCodec));

            Assert.IsTrue(builder.AnimeMask.FirstByteFlags.HasFlag(FileAnimeMaskFirstByte.Type));
            Assert.IsTrue(builder.AnimeMask.FirstByteFlags.HasFlag(FileAnimeMaskFirstByte.HighestEpisodeNumber));
            Assert.IsTrue(builder.AnimeMask.FirstByteFlags.HasFlag(FileAnimeMaskFirstByte.TotalEpisodes));
            Assert.IsTrue(builder.AnimeMask.SecondByteFlags.HasFlag(FileAnimeMaskSecondByte.RomajiName));
            Assert.IsTrue(builder.AnimeMask.ThirdByteFlags.HasFlag(FileAnimeMaskThirdByte.EpisodeNumber));
            Assert.IsTrue(builder.AnimeMask.ThirdByteFlags.HasFlag(FileAnimeMaskThirdByte.EpisodeName));

            path = builder.BuildPath(fileInfo, animeInfo, PlatformUtils.MaxPathLength);

            Assert.AreEqual(
                $"testRoot{Path.DirectorySeparatorChar}tv{Path.DirectorySeparatorChar}Koukaku Kidoutai S.A.C. 2nd GIG{Path.DirectorySeparatorChar}Koukaku Kidoutai S.A.C. 2nd GIG - 02v2 - Night Cruise[Hi10][1280x688][h264][D7083952]",
                path);

            path = builder.BuildPath(fileInfo, animeInfo, 127);

            Assert.AreEqual($"testRoot{Path.DirectorySeparatorChar}tv{Path.DirectorySeparatorChar}Koukaku Kidoutai S.A.C. 2nd GIG{Path.DirectorySeparatorChar}Koukaku Kidoutai S.A.C. 2nd GIG - 02v2 - Night C...[Hi10][1280x688][h264][D7083952]",
                path);

            animeInfo.EpisodeName = "Night Cruise...";

            path = builder.BuildPath(fileInfo, animeInfo, PlatformUtils.MaxPathLength);

            Assert.AreEqual(
                $"testRoot{Path.DirectorySeparatorChar}tv{Path.DirectorySeparatorChar}Koukaku Kidoutai S.A.C. 2nd GIG{Path.DirectorySeparatorChar}Koukaku Kidoutai S.A.C. 2nd GIG - 02v2 - Night Cruise_[Hi10][1280x688][h264][D7083952]",
                path);
        }
    }
}