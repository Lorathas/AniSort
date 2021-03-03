using System;
using AniDbSharp.Extensions;

namespace AniDbSharp.Data
{
    /// <summary>
    /// Class to contain info of an anime mask for requesting anime data from the server
    /// Flags are configured to be based off of https://wiki.anidb.net/UDP_API_Definition#ANIME:_Retrieve_Anime_Data
    /// </summary>
    public class AnimeMask
    {
        public AnimeMaskFirstByte FirstByteFlags { get; }
        public AnimeMaskSecondByte SecondByteFlags { get; }
        public AnimeMaskThirdByte ThirdByteFlags { get; }
        public AnimeMaskFourthByte FourthByteFlags { get; }
        public AnimeMaskFifthByte FifthByteFlags { get; }
        public AnimeMaskSixthByte SixthByteFlags { get; }
        public AnimeMaskSeventhByte SeventhByteFlags { get; }

        public AnimeMask(AnimeMaskFirstByte firstByteFlags, AnimeMaskSecondByte secondByteFlags, AnimeMaskThirdByte thirdByteFlags, AnimeMaskFourthByte fourthByteFlags, AnimeMaskFifthByte fifthByteFlags,
            AnimeMaskSixthByte sixthByteFlags, AnimeMaskSeventhByte seventhByteFlags)
        {
            FirstByteFlags = firstByteFlags;
            SecondByteFlags = secondByteFlags;
            ThirdByteFlags = thirdByteFlags;
            FourthByteFlags = fourthByteFlags;
            FifthByteFlags = fifthByteFlags;
            SixthByteFlags = sixthByteFlags;
            SeventhByteFlags = seventhByteFlags;
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

        /// <summary>
        /// Parse anime bytes from hex string
        /// </summary>
        /// <param name="hex"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static AnimeMask FromHexString(string hex)
        {
            byte[] bytes = hex.HexStringToBytes();

            if (bytes.Length > 7 || bytes.Length <= 0)
            {
                throw new Exception("Invalid hex string result");
            }

            AnimeMaskFirstByte firstByte = 0;
            AnimeMaskSecondByte secondByte = 0;
            AnimeMaskThirdByte thirdByte = 0;
            AnimeMaskFourthByte fourthByte = 0;
            AnimeMaskFifthByte fifthByte = 0;
            AnimeMaskSixthByte sixthByte = 0;
            AnimeMaskSeventhByte seventhByte = 0;
            
            if (bytes.Length >= 1)
            {
                firstByte = (AnimeMaskFirstByte) bytes[0];
            }
            if (bytes.Length >= 2)
            {
                secondByte = (AnimeMaskSecondByte) bytes[1];
            }
            if (bytes.Length >= 3)
            {
                thirdByte = (AnimeMaskThirdByte) bytes[2];
            }
            if (bytes.Length >= 4)
            {
                fourthByte = (AnimeMaskFourthByte) bytes[3];
            }
            if (bytes.Length >= 5)
            {
                fifthByte = (AnimeMaskFifthByte) bytes[4];
            }
            if (bytes.Length >= 6)
            {
                sixthByte = (AnimeMaskSixthByte) bytes[5];
            }
            if (bytes.Length == 7)
            {
                seventhByte = (AnimeMaskSeventhByte) bytes[6];
            }

            return new AnimeMask(firstByte, secondByte, thirdByte, fourthByte, fifthByte, sixthByte, seventhByte);
        }
    }

    [Flags]
    public enum AnimeMaskFirstByte : byte
    {
        AniDbId = 128,
        DateFlags = 64,
        Year = 32,
        Type = 16,
        RelatedAnimeIdList = 8,
        RelatedAnimeTypeList = 4
    }

    [Flags]
    public enum AnimeMaskSecondByte : byte
    {
        RomajiName = 128,
        KanjiName = 64,
        EnglishName = 32,
        OtherName = 16,
        ShortNameList = 8,
        SynonymList = 4
    }

    [Flags]
    public enum AnimeMaskThirdByte : byte
    {
        EpisodeCount = 128,
        HighestEpisodeNumber = 64,
        SpecialEpisodeCount = 32,
        AirDate = 16,
        EndDate = 8,
        Url = 4,
        PictureName = 2
    }

    [Flags]
    public enum AnimeMaskFourthByte : byte
    {
        Rating = 128,
        VoteCount = 64,
        TempRating = 32,
        TempVoteCount = 16,
        AverageReviewRating = 8,
        ReviewCount = 4,
        AwardList = 2,
        Is18PlusRestricted = 1
    }

    [Flags]
    public enum AnimeMaskFifthByte : byte
    {
        AnimeNewNetworkId = 64,
        AllCinemaId = 32,
        AnimeNfo = 16,
        TagNameList = 8,
        TagIdList = 4,
        TagWeightList = 2,
        UpdatedAt = 1
    }

    [Flags]
    public enum AnimeMaskSixthByte : byte
    {
        CharacterIdList = 128
    }

    [Flags]
    public enum AnimeMaskSeventhByte : byte
    {
        SpecialsCount = 128,
        CreditsCount = 64,
        OtherCount = 32,
        TrailersCount = 16,
        ParodiesCount = 8
    }
}
