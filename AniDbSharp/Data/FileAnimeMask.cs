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

using System;
using AniDbSharp.Extensions;

namespace AniDbSharp.Data
{
    public record FileAnimeMask(
        FileAnimeMaskFirstByte FirstByteFlags,
        FileAnimeMaskSecondByte SecondByteFlags,
        FileAnimeMaskThirdByte ThirdByteFlags,
        FileAnimeMaskFourthByte FourthByteFlags)
    {
        public FileAnimeMask() : this(0, 0, 0, 0)
        {
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