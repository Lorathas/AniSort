using Microsoft.VisualStudio.TestTools.UnitTesting;
using AniDbSharp.Data;
using System;
using System.Collections.Generic;
using System.Text;

namespace AniDbSharp.Data.Tests
{
    [TestClass()]
    public class FileMaskTests
    {
        [TestMethod()]
        public void FromHexStringTest()
        {
            var mask = FileMask.FromHexString("7FF8FEF8");

            Assert.AreEqual(FileMaskFirstByte.AnimeId, mask.FirstByteFlags & FileMaskFirstByte.AnimeId);
            Assert.AreEqual(FileMaskFirstByte.EpisodeId, mask.FirstByteFlags & FileMaskFirstByte.EpisodeId);
            Assert.AreEqual(FileMaskFirstByte.GroupId, mask.FirstByteFlags & FileMaskFirstByte.GroupId);
            Assert.AreEqual(FileMaskFirstByte.MyListId, mask.FirstByteFlags & FileMaskFirstByte.MyListId);
            Assert.AreEqual(FileMaskFirstByte.OtherEpisodes, mask.FirstByteFlags & FileMaskFirstByte.OtherEpisodes);
            Assert.AreEqual(FileMaskFirstByte.IsDeprecated, mask.FirstByteFlags & FileMaskFirstByte.IsDeprecated);
            Assert.AreEqual(FileMaskFirstByte.State, mask.FirstByteFlags & FileMaskFirstByte.State);

            Assert.AreEqual(FileMaskSecondByte.Size, mask.SecondByteFlags & FileMaskSecondByte.Size);
            Assert.AreEqual(FileMaskSecondByte.Ed2k, mask.SecondByteFlags & FileMaskSecondByte.Ed2k);
            Assert.AreEqual(FileMaskSecondByte.Md5, mask.SecondByteFlags & FileMaskSecondByte.Md5);
            Assert.AreEqual(FileMaskSecondByte.Sha1, mask.SecondByteFlags & FileMaskSecondByte.Sha1);
            Assert.AreEqual(FileMaskSecondByte.Crc32, mask.SecondByteFlags & FileMaskSecondByte.Crc32);
            Assert.AreEqual((FileMaskSecondByte) 0, mask.SecondByteFlags & FileMaskSecondByte.VideoColorDepth);

            Assert.AreEqual(FileMaskThirdByte.Quality, mask.ThirdByteFlags & FileMaskThirdByte.Quality);
            Assert.AreEqual(FileMaskThirdByte.Source, mask.ThirdByteFlags & FileMaskThirdByte.Source);
            Assert.AreEqual(FileMaskThirdByte.AudioCodecList, mask.ThirdByteFlags & FileMaskThirdByte.AudioCodecList);
            Assert.AreEqual(FileMaskThirdByte.AudioBitrateList, mask.ThirdByteFlags & FileMaskThirdByte.AudioBitrateList);
            Assert.AreEqual(FileMaskThirdByte.VideoCodec, mask.ThirdByteFlags & FileMaskThirdByte.VideoCodec);
            Assert.AreEqual(FileMaskThirdByte.VideoBitrate, mask.ThirdByteFlags & FileMaskThirdByte.VideoBitrate);
            Assert.AreEqual(FileMaskThirdByte.VideoResolution, mask.ThirdByteFlags & FileMaskThirdByte.VideoResolution);
            Assert.AreEqual((FileMaskThirdByte) 0, mask.ThirdByteFlags & FileMaskThirdByte.FileType);

            Assert.AreEqual(FileMaskFourthByte.DubLanguage, mask.FourthByteFlags & FileMaskFourthByte.DubLanguage);
            Assert.AreEqual(FileMaskFourthByte.SubLanguage, mask.FourthByteFlags & FileMaskFourthByte.SubLanguage);
            Assert.AreEqual(FileMaskFourthByte.LengthInSeconds, mask.FourthByteFlags & FileMaskFourthByte.LengthInSeconds);
            Assert.AreEqual(FileMaskFourthByte.Description, mask.FourthByteFlags & FileMaskFourthByte.Description);
            Assert.AreEqual(FileMaskFourthByte.AiredDate, mask.FourthByteFlags & FileMaskFourthByte.AiredDate);
            Assert.AreEqual((FileMaskFourthByte) 0, mask.FourthByteFlags & FileMaskFourthByte.AniDbFilename);

            Assert.AreEqual((FileMaskFifthByte) 0, mask.FifthByteFlags & FileMaskFifthByte.MyListFileState);
            Assert.AreEqual((FileMaskFifthByte) 0, mask.FifthByteFlags & FileMaskFifthByte.MyListFileState);
            Assert.AreEqual((FileMaskFifthByte) 0, mask.FifthByteFlags & FileMaskFifthByte.MyListViewed);
            Assert.AreEqual((FileMaskFifthByte) 0, mask.FifthByteFlags & FileMaskFifthByte.MyListViewDate);
            Assert.AreEqual((FileMaskFifthByte) 0, mask.FifthByteFlags & FileMaskFifthByte.MyListStorage);
            Assert.AreEqual((FileMaskFifthByte) 0, mask.FifthByteFlags & FileMaskFifthByte.MyListSource);
            Assert.AreEqual((FileMaskFifthByte) 0, mask.FifthByteFlags & FileMaskFifthByte.MyListOther);
        }
    }
}