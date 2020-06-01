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

using Microsoft.VisualStudio.TestTools.UnitTesting;
using AniDbSharp.Data;
using System;
using System.Collections.Generic;
using System.Text;
using AniDbSharp.Extensions;

namespace AniDbSharp.Data.Tests
{
    [TestClass()]
    public class FileResultTests
    {
        [TestMethod()]
        public void ParseTest()
        {
            var fileMask = FileMask.FromHexString("7FF8FEF8");
            var animeMask = FileAnimeMask.FromHexString("C000F0C0");
            string testData =
                "312498|" +
                "4688|" +
                "69260|" +
                "4243|" +
                "0|" +
                "|" +
                "0|" +
                "1|" +
                "177747474|" +
                "70cd93fd3981cc80a8ea6a646ff805c9|" +
                "b2a7c7d591333e20495de3571b235c28|" +
                "7af9b962c17ff729baeee67533e5219526cd5095|" +
                "a200fe73|" +
                "high|" +
                "DTV|" +
                "Vorbis (Ogg Vorbis)|" +
                "104|" +
                "H264/AVC|" +
                "800|" +
                "704x400|" +
                "japanese|" +
                "english'english'english|" +
                "1560|" +
                "|" +
                "1175472000|" +
                "26|" +
                "26|" +
                "01|" +
                "The Wings to the Sky|" +
                "Sora he no Tsubasa|" +
                "????|" +
                "#nanoha-DamagedGoodz|" +
                "Nanoha-DGz";

            var fileResult = new FileResult(true, null);

            fileResult.Parse(testData, fileMask, animeMask);

            Assert.IsNotNull(fileResult.FileInfo);
            Assert.IsNotNull(fileResult.AnimeInfo);

            Assert.AreEqual(312498, fileResult.FileInfo.FileId);

            Assert.AreEqual(4688, fileResult.FileInfo.AnimeId);
            Assert.AreEqual(69260, fileResult.FileInfo.EpisodeId);
            Assert.AreEqual(4243, fileResult.FileInfo.GroupId);
            Assert.AreEqual(0, fileResult.FileInfo.MyListId);
            Assert.AreEqual(string.Empty, fileResult.FileInfo.OtherEpisodes);
            Assert.AreEqual((short) 0, fileResult.FileInfo.IsDeprecated);
            Assert.AreEqual(FileState.CrcOk, fileResult.FileInfo.State);

            Assert.AreEqual(177747474, fileResult.FileInfo.Size);
            CollectionAssert.AreEqual("70cd93fd3981cc80a8ea6a646ff805c9".HexStringToBytes(), fileResult.FileInfo.Ed2kHash);
            CollectionAssert.AreEqual("b2a7c7d591333e20495de3571b235c28".HexStringToBytes(), fileResult.FileInfo.Md5Hash);
            CollectionAssert.AreEqual("7af9b962c17ff729baeee67533e5219526cd5095".HexStringToBytes(), fileResult.FileInfo.Sha1Hash);
            CollectionAssert.AreEqual("a200fe73".HexStringToBytes(), fileResult.FileInfo.Crc32Hash);
            Assert.IsNull(fileResult.FileInfo.VideoColorDepth);

            Assert.AreEqual("high", fileResult.FileInfo.Quality);
            Assert.AreEqual("DTV", fileResult.FileInfo.Source);
            Assert.AreEqual("Vorbis (Ogg Vorbis)", fileResult.FileInfo.AudioCodecList);
            Assert.AreEqual(104, fileResult.FileInfo.AudioBitrateList);
            Assert.AreEqual("H264|AVC", fileResult.FileInfo.VideoCodec);
            Assert.AreEqual(800, fileResult.FileInfo.VideoBitrate);
            Assert.AreEqual("704x400", fileResult.FileInfo.VideoResolution);
            Assert.IsNull(fileResult.FileInfo.FileType);

            Assert.AreEqual("japanese", fileResult.FileInfo.DubLanguage);
            Assert.AreEqual("english'english'english", fileResult.FileInfo.SubLanguage);
            Assert.AreEqual(1560, fileResult.FileInfo.LengthInSeconds);
            Assert.AreEqual(string.Empty, fileResult.FileInfo.Description);
            Assert.AreEqual(1175472000, fileResult.FileInfo.AiredDate);

            Assert.AreEqual(26, fileResult.AnimeInfo.TotalEpisodes);
            Assert.AreEqual(26, fileResult.AnimeInfo.HighestEpisodeNumber);
            Assert.AreEqual("The Wings to the Sky", fileResult.AnimeInfo.EpisodeName);
            Assert.AreEqual("Sora he no Tsubasa", fileResult.AnimeInfo.EpisodeRomajiName);
            Assert.AreEqual("????", fileResult.AnimeInfo.EpisodeKanjiName);
            Assert.AreEqual("#nanoha-DamagedGoodz", fileResult.AnimeInfo.GroupName);
            Assert.AreEqual("Nanoha-DGz", fileResult.AnimeInfo.GroupShortName);
        }
    }
}