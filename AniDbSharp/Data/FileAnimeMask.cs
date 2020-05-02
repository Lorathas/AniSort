using System;
using System.Collections.Generic;
using System.Text;
using AniDbSharp.Extensions;

namespace AniDbSharp.Data
{
    public class FileAnimeMask
    {
        public FileAnimeMaskFirstByte FirstByteFlags { get; }
        public FileAnimeMaskSecondByte SecondByteFlags { get; }
        public FileAnimeMaskThirdByte ThirdByteFlags { get; }
        public FileAnimeMaskFourthByte FourthByteFlags { get; }

        public FileAnimeMask(FileAnimeMaskFirstByte firstByteFlags, FileAnimeMaskSecondByte secondByteFlags,
            FileAnimeMaskThirdByte thirdByteFlags, FileAnimeMaskFourthByte fourthByteFlags)
        {
            FirstByteFlags = firstByteFlags;
            SecondByteFlags = secondByteFlags;
            ThirdByteFlags = thirdByteFlags;
            FourthByteFlags = fourthByteFlags;
        }

        /// <summary>
        /// Generate anime mask bytes to send to the server
        /// </summary>
        /// <returns>Array of bytes representing the file mask flags</returns>
        public byte[] GenerateBytes()
        {
            return new[]
            {
                (byte) FirstByteFlags, (byte) SecondByteFlags, (byte) ThirdByteFlags, (byte) FourthByteFlags
            };
        }

        public static FileAnimeMask FromHexString(string hex)
        {
            byte[] bytes = hex.HexStringToBytes();

            if (bytes.Length > 4 || bytes.Length <= 0)
            {
                throw new Exception("Invalid hex string result");
            }
            else if (bytes.Length == 4)
            {
                return new FileAnimeMask((FileAnimeMaskFirstByte) bytes[0], (FileAnimeMaskSecondByte) bytes[1],
                    (FileAnimeMaskThirdByte) bytes[2], (FileAnimeMaskFourthByte) bytes[3]);
            }
            else if (bytes.Length == 3)
            {
                return new FileAnimeMask((FileAnimeMaskFirstByte) bytes[0], (FileAnimeMaskSecondByte) bytes[1],
                    (FileAnimeMaskThirdByte) bytes[2], 0);
            }
            else if (bytes.Length == 2)
            {
                return new FileAnimeMask((FileAnimeMaskFirstByte) bytes[0], (FileAnimeMaskSecondByte) bytes[1], 0, 0);
            }
            else
            {
                return new FileAnimeMask((FileAnimeMaskFirstByte) bytes[0], 0, 0, 0);
            }
        }
    }

    [Flags]
    public enum FileAnimeMaskFirstByte
    {
        TotalEpisodes = 128,
        HighestEpisodeNumber = 64,
        Year = 32,
        Type = 16,
        RelatedAnimeIdList = 8,
        RelatedAnimeIdType = 4,
        CategoryList = 2
    }

    [Flags]
    public enum FileAnimeMaskSecondByte
    {
        RomajiName = 128,
        KanjiName = 64,
        EnglishName = 32,
        OtherName = 16,
        ShortNameList = 8,
        SynonymList = 4
    }

    [Flags]
    public enum FileAnimeMaskThirdByte
    {
        EpisodeNumber = 128,
        EpisodeName = 64,
        EpisodeRomajiName = 32,
        EpisodeKanjiName = 16,
        EpisodeRating = 8,
        EpisodeVoteCount = 4
    }

    [Flags]
    public enum FileAnimeMaskFourthByte
    {
        GroupName = 128,
        GroupShortName = 64,
        DateAnimeIdRecordUpdated = 1
    }
}