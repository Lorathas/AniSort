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