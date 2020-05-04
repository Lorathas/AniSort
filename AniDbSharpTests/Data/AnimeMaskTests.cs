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

namespace AniDbSharp.Data.Tests
{
    [TestClass()]
    public class AnimeMaskTests
    {
        [TestMethod()]
        public void FromHexStringTest()
        {
            var mask = FileAnimeMask.FromHexString("C000F0C0");

            Assert.AreEqual(FileAnimeMaskFirstByte.TotalEpisodes, mask.FirstByteFlags & FileAnimeMaskFirstByte.TotalEpisodes);
            Assert.AreEqual(FileAnimeMaskFirstByte.HighestEpisodeNumber, mask.FirstByteFlags & FileAnimeMaskFirstByte.HighestEpisodeNumber);
            Assert.AreEqual((FileAnimeMaskFirstByte) 0, mask.FirstByteFlags & FileAnimeMaskFirstByte.Year);
            Assert.AreEqual((FileAnimeMaskFirstByte) 0, mask.FirstByteFlags & FileAnimeMaskFirstByte.Type);
            Assert.AreEqual((FileAnimeMaskFirstByte) 0, mask.FirstByteFlags & FileAnimeMaskFirstByte.RelatedAnimeIdList);
            Assert.AreEqual((FileAnimeMaskFirstByte) 0, mask.FirstByteFlags & FileAnimeMaskFirstByte.RelatedAnimeIdType);
            Assert.AreEqual((FileAnimeMaskFirstByte) 0, mask.FirstByteFlags & FileAnimeMaskFirstByte.CategoryList);

            Assert.AreEqual((FileAnimeMaskSecondByte) 0, mask.SecondByteFlags & FileAnimeMaskSecondByte.RomajiName);
            Assert.AreEqual((FileAnimeMaskSecondByte) 0, mask.SecondByteFlags & FileAnimeMaskSecondByte.KanjiName);
            Assert.AreEqual((FileAnimeMaskSecondByte) 0, mask.SecondByteFlags & FileAnimeMaskSecondByte.EnglishName);
            Assert.AreEqual((FileAnimeMaskSecondByte) 0, mask.SecondByteFlags & FileAnimeMaskSecondByte.OtherName);
            Assert.AreEqual((FileAnimeMaskSecondByte) 0, mask.SecondByteFlags & FileAnimeMaskSecondByte.ShortNameList);
            Assert.AreEqual((FileAnimeMaskSecondByte) 0, mask.SecondByteFlags & FileAnimeMaskSecondByte.SynonymList);

            Assert.AreEqual(FileAnimeMaskThirdByte.EpisodeNumber, mask.ThirdByteFlags & FileAnimeMaskThirdByte.EpisodeNumber);
            Assert.AreEqual(FileAnimeMaskThirdByte.EpisodeName, mask.ThirdByteFlags & FileAnimeMaskThirdByte.EpisodeName);
            Assert.AreEqual(FileAnimeMaskThirdByte.EpisodeRomajiName, mask.ThirdByteFlags & FileAnimeMaskThirdByte.EpisodeRomajiName);
            Assert.AreEqual(FileAnimeMaskThirdByte.EpisodeKanjiName, mask.ThirdByteFlags & FileAnimeMaskThirdByte.EpisodeKanjiName);
            Assert.AreEqual((FileAnimeMaskThirdByte) 0, mask.ThirdByteFlags & FileAnimeMaskThirdByte.EpisodeRating);
            Assert.AreEqual((FileAnimeMaskThirdByte) 0, mask.ThirdByteFlags & FileAnimeMaskThirdByte.EpisodeVoteCount);

            Assert.AreEqual(FileAnimeMaskFourthByte.GroupName, mask.FourthByteFlags & FileAnimeMaskFourthByte.GroupName);
            Assert.AreEqual(FileAnimeMaskFourthByte.GroupShortName, mask.FourthByteFlags & FileAnimeMaskFourthByte.GroupShortName);
            Assert.AreEqual((FileAnimeMaskFourthByte) 0, mask.FourthByteFlags & FileAnimeMaskFourthByte.DateAnimeIdRecordUpdated);
        }
    }
}