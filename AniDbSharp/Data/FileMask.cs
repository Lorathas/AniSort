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

using System;
using AniDbSharp.Extensions;

namespace AniDbSharp.Data
{
    public class FileMask
    {
        public FileMaskFirstByte FirstByteFlags { get; }
        public FileMaskSecondByte SecondByteFlags { get; }
        public FileMaskThirdByte ThirdByteFlags { get; }
        public FileMaskFourthByte FourthByteFlags { get; }
        public FileMaskFifthByte FifthByteFlags { get; }

        public FileMask(FileMaskFirstByte firstByteFlags, FileMaskSecondByte secondByteFlags,
            FileMaskThirdByte thirdByteFlags, FileMaskFourthByte fourthByteFlags, FileMaskFifthByte fifthByteFlags)
        {
            FirstByteFlags = firstByteFlags;
            SecondByteFlags = secondByteFlags;
            ThirdByteFlags = thirdByteFlags;
            FourthByteFlags = fourthByteFlags;
            FifthByteFlags = fifthByteFlags;
        }

        /// <summary>
        /// Generate file mask bytes to send to the server
        /// </summary>
        /// <returns>Array of bytes representing the file mask flags</returns>
        public byte[] GenerateBytes()
        {
            return new[]
            {
                (byte) FirstByteFlags, (byte) SecondByteFlags, (byte) ThirdByteFlags, (byte) FourthByteFlags,
                (byte) FifthByteFlags
            };
        }

        public static FileMask FromHexString(string hex)
        {
            byte[] bytes = hex.HexStringToBytes();

            if (bytes.Length > 5 || bytes.Length <= 0)
            {
                throw new Exception("Invalid hex string result");
            }
            else if (bytes.Length == 5)
            {
                return new FileMask((FileMaskFirstByte) bytes[0], (FileMaskSecondByte) bytes[1],
                    (FileMaskThirdByte) bytes[2], (FileMaskFourthByte) bytes[3], (FileMaskFifthByte) bytes[4]);
            }
            else if (bytes.Length == 4)
            {
                return new FileMask((FileMaskFirstByte) bytes[0], (FileMaskSecondByte) bytes[1],
                    (FileMaskThirdByte) bytes[2], (FileMaskFourthByte) bytes[3], 0);
            }
            else if (bytes.Length == 3)
            {
                return new FileMask((FileMaskFirstByte) bytes[0], (FileMaskSecondByte) bytes[1],
                    (FileMaskThirdByte) bytes[2], 0, 0);
            }
            else if (bytes.Length == 2)
            {
                return new FileMask((FileMaskFirstByte) bytes[0], (FileMaskSecondByte) bytes[1], 0, 0, 0);
            }
            else
            {
                return new FileMask((FileMaskFirstByte) bytes[0], 0, 0, 0, 0);
            }
        }
    }

    [Flags]
    public enum FileMaskFirstByte : byte
    {
        AnimeId = 64,
        EpisodeId = 32,
        GroupId = 16,
        MyListId = 8,
        OtherEpisodes = 4,
        IsDeprecated = 2,
        State = 1
    }

    [Flags]
    public enum FileMaskSecondByte : byte
    {
        Size = 128,
        Ed2k = 64,
        Md5 = 32,
        Sha1 = 16,
        Crc32 = 8,
        VideoColorDepth = 2
    }

    [Flags]
    public enum FileMaskThirdByte
    {
        Quality = 128,
        Source = 64,
        AudioCodecList = 32,
        AudioBitrateList = 16,
        VideoCodec = 8,
        VideoBitrate = 4,
        VideoResolution = 2,
        FileType = 1
    }

    [Flags]
    public enum FileMaskFourthByte
    {
        DubLanguage = 128,
        SubLanguage = 64,
        LengthInSeconds = 32,
        Description = 16,
        AiredDate = 8,
        AniDbFilename = 1
    }

    [Flags]
    public enum FileMaskFifthByte
    {
        MyListState = 128,
        MyListFileState = 64,
        MyListViewed = 32,
        MyListViewDate = 16,
        MyListStorage = 8,
        MyListSource = 4,
        MyListOther = 2
    }
}