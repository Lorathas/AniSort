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
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Net.NetworkInformation;
using System.Text;
using AniDbSharp.Data;
using AniSort.Core.Exceptions;
using AniSort.Core.Extensions;

namespace AniSort.Core.IO
{
    internal static class FileFormatParser
    {
        private delegate void FlagModifier(ref FileMaskFirstByte fb0, ref FileMaskSecondByte fb1,
            ref FileMaskThirdByte fb2, ref FileMaskFourthByte fb3, ref FileMaskFifthByte fb4,
            ref FileAnimeMaskFirstByte ab0, ref FileAnimeMaskSecondByte ab1, ref FileAnimeMaskThirdByte ab2,
            ref FileAnimeMaskFourthByte ab3);

        private static readonly Dictionary<string, Func<string, string, bool, PredicateFileFormatEmitter>>
            VariableFormatters
                = new Dictionary<string, Func<string, string, bool, PredicateFileFormatEmitter>>
                {
                    {
                        "animeEnglish",
                        (prefix, suffix, ellipsize) =>
                            new PredicateFileFormatEmitter((f, a) => a.EnglishName, prefix, suffix, ellipsize)
                    },
                    {
                        "animeRomaji",
                        (prefix, suffix, ellipsize) =>
                            new PredicateFileFormatEmitter((f, a) => a.RomajiName, prefix, suffix, ellipsize)
                    },
                    {
                        "animeKanji",
                        (prefix, suffix, ellipsize) =>
                            new PredicateFileFormatEmitter((f, a) => a.KanjiName, prefix, suffix, ellipsize)
                    },
                    {
                        "episodeNumber",
                        (prefix, suffix, ellipsize) => new PredicateFileFormatEmitter((f, a) =>
                        {
                            int paddingDigits = 6;
                            for (int idx = 2; idx < 6; idx++)
                            {
                                if (Math.Pow(10, idx) > a.HighestEpisodeNumber)
                                {
                                    paddingDigits = idx;
                                    break;
                                }
                            }

                            if (a.EpisodeNumber.Length < paddingDigits)
                            {
                                var builder = new StringBuilder();

                                for (int idx = 0; idx < paddingDigits - a.EpisodeNumber.Length; idx++)
                                {
                                    builder.Append('0');
                                }

                                builder.Append(a.EpisodeNumber);

                                return builder.ToString();
                            }
                            else
                            {
                                return a.EpisodeNumber;
                            }
                        }, prefix, suffix, ellipsize)
                    },
                    {
                        "fileVersion",
                        (prefix, suffix, ellipsize) => new PredicateFileFormatEmitter((f, a) =>
                        {
                            if (f.State == null)
                            {
                                return null;
                            }

                            if (f.State.Value.HasFlag(FileState.IsVersion5))
                            {
                                return "5";
                            }
                            else if (f.State.Value.HasFlag(FileState.IsVersion4))
                            {
                                return "4";
                            }
                            else if (f.State.Value.HasFlag(FileState.IsVersion3))
                            {
                                return "3";
                            }
                            else if (f.State.Value.HasFlag(FileState.IsVersion2))
                            {
                                return "2";
                            }
                            else
                            {
                                return null;
                            }
                        }, prefix, suffix, ellipsize)
                    },
                    {
                        "episodeEnglish",
                        (prefix, suffix, ellipsize) =>
                            new PredicateFileFormatEmitter((f, a) => a.EpisodeName, prefix, suffix, ellipsize)
                    },
                    {
                        "episodeRomaji",
                        (prefix, suffix, ellipsize) =>
                            new PredicateFileFormatEmitter((f, a) => a.EpisodeRomajiName, prefix, suffix, ellipsize)
                    },
                    {
                        "episodeKanji",
                        (prefix, suffix, ellipsize) =>
                            new PredicateFileFormatEmitter((f, a) => a.EpisodeKanjiName, prefix, suffix, ellipsize)
                    },
                    {
                        "group",
                        (prefix, suffix, ellipsize) =>
                            new PredicateFileFormatEmitter((f, a) => a.GroupName, prefix, suffix, ellipsize)
                    },
                    {
                        "groupShort",
                        (prefix, suffix, ellipsize) =>
                            new PredicateFileFormatEmitter((f, a) => a.GroupShortName, prefix, suffix, ellipsize)
                    },
                    {
                        "resolution",
                        (prefix, suffix, ellipsize) =>
                            new PredicateFileFormatEmitter((f, a) => f.VideoResolution, prefix, suffix, ellipsize)
                    },
                    {
                        "videoCodec",
                        (prefix, suffix, ellipsize) =>
                            new PredicateFileFormatEmitter((f, a) => f.VideoCodec, prefix, suffix, ellipsize)
                    },
                    {
                        "crc32",
                        (prefix, suffix, ellipsize) =>
                            new PredicateFileFormatEmitter((f, a) => f.Crc32Hash.ToHexString(), prefix, suffix,
                                ellipsize)
                    },
                    {
                        "ed2k",
                        (prefix, suffix, ellipsize) =>
                            new PredicateFileFormatEmitter((f, a) => f.Ed2kHash.ToHexString(), prefix, suffix,
                                ellipsize)
                    },
                    {
                        "md5",
                        (prefix, suffix, ellipsize) => new PredicateFileFormatEmitter((f, a) => f.Md5Hash.ToHexString(),
                            prefix, suffix, ellipsize)
                    },
                    {
                        "sha1",
                        (prefix, suffix, ellipsize) =>
                            new PredicateFileFormatEmitter((f, a) => f.Sha1Hash.ToHexString(), prefix, suffix,
                                ellipsize)
                    },
                };

        private static readonly Dictionary<string, FlagModifier> FlagModifiers =
            new Dictionary<string, FlagModifier>
            {
                {
                    "animeEnglish",
                    (ref FileMaskFirstByte fb0, ref FileMaskSecondByte fb1, ref FileMaskThirdByte fb2,
                        ref FileMaskFourthByte fb3, ref FileMaskFifthByte fb4, ref FileAnimeMaskFirstByte ab0,
                        ref FileAnimeMaskSecondByte ab1, ref FileAnimeMaskThirdByte ab2,
                        ref FileAnimeMaskFourthByte ab3) => ab1 |= FileAnimeMaskSecondByte.EnglishName
                },
                {
                    "animeRomaji",
                    (ref FileMaskFirstByte fb0, ref FileMaskSecondByte fb1, ref FileMaskThirdByte fb2,
                        ref FileMaskFourthByte fb3, ref FileMaskFifthByte fb4, ref FileAnimeMaskFirstByte ab0,
                        ref FileAnimeMaskSecondByte ab1, ref FileAnimeMaskThirdByte ab2,
                        ref FileAnimeMaskFourthByte ab3) => ab1 |= FileAnimeMaskSecondByte.RomajiName
                },
                {
                    "animeKanji",
                    (ref FileMaskFirstByte fb0, ref FileMaskSecondByte fb1, ref FileMaskThirdByte fb2,
                        ref FileMaskFourthByte fb3, ref FileMaskFifthByte fb4, ref FileAnimeMaskFirstByte ab0,
                        ref FileAnimeMaskSecondByte ab1, ref FileAnimeMaskThirdByte ab2,
                        ref FileAnimeMaskFourthByte ab3) => ab1 |= FileAnimeMaskSecondByte.KanjiName
                },
                {
                    "episodeNumber",
                    (ref FileMaskFirstByte fb0, ref FileMaskSecondByte fb1, ref FileMaskThirdByte fb2,
                        ref FileMaskFourthByte fb3, ref FileMaskFifthByte fb4, ref FileAnimeMaskFirstByte ab0,
                        ref FileAnimeMaskSecondByte ab1, ref FileAnimeMaskThirdByte ab2,
                        ref FileAnimeMaskFourthByte ab3) =>
                    {
                        ab2 |= FileAnimeMaskThirdByte.EpisodeNumber;
                        ab0 |= FileAnimeMaskFirstByte.TotalEpisodes | FileAnimeMaskFirstByte.HighestEpisodeNumber;
                    }
                },
                {
                    "fileVersion",
                    (ref FileMaskFirstByte fb0, ref FileMaskSecondByte fb1, ref FileMaskThirdByte fb2,
                        ref FileMaskFourthByte fb3, ref FileMaskFifthByte fb4, ref FileAnimeMaskFirstByte ab0,
                        ref FileAnimeMaskSecondByte ab1, ref FileAnimeMaskThirdByte ab2,
                        ref FileAnimeMaskFourthByte ab3) => fb0 |= FileMaskFirstByte.State
                },
                {
                    "episodeEnglish",
                    (ref FileMaskFirstByte fb0, ref FileMaskSecondByte fb1, ref FileMaskThirdByte fb2,
                        ref FileMaskFourthByte fb3, ref FileMaskFifthByte fb4, ref FileAnimeMaskFirstByte ab0,
                        ref FileAnimeMaskSecondByte ab1, ref FileAnimeMaskThirdByte ab2,
                        ref FileAnimeMaskFourthByte ab3) => ab2 |= FileAnimeMaskThirdByte.EpisodeName
                },
                {
                    "episodeRomaji",
                    (ref FileMaskFirstByte fb0, ref FileMaskSecondByte fb1, ref FileMaskThirdByte fb2,
                        ref FileMaskFourthByte fb3, ref FileMaskFifthByte fb4, ref FileAnimeMaskFirstByte ab0,
                        ref FileAnimeMaskSecondByte ab1, ref FileAnimeMaskThirdByte ab2,
                        ref FileAnimeMaskFourthByte ab3) => ab2 |= FileAnimeMaskThirdByte.EpisodeRomajiName
                },
                {
                    "episodeKanji",
                    (ref FileMaskFirstByte fb0, ref FileMaskSecondByte fb1, ref FileMaskThirdByte fb2,
                        ref FileMaskFourthByte fb3, ref FileMaskFifthByte fb4, ref FileAnimeMaskFirstByte ab0,
                        ref FileAnimeMaskSecondByte ab1, ref FileAnimeMaskThirdByte ab2,
                        ref FileAnimeMaskFourthByte ab3) => ab2 |= FileAnimeMaskThirdByte.EpisodeKanjiName
                },
                {
                    "group",
                    (ref FileMaskFirstByte fb0, ref FileMaskSecondByte fb1, ref FileMaskThirdByte fb2,
                        ref FileMaskFourthByte fb3, ref FileMaskFifthByte fb4, ref FileAnimeMaskFirstByte ab0,
                        ref FileAnimeMaskSecondByte ab1, ref FileAnimeMaskThirdByte ab2,
                        ref FileAnimeMaskFourthByte ab3) => ab3 |= FileAnimeMaskFourthByte.GroupName
                },
                {
                    "groupShort",
                    (ref FileMaskFirstByte fb0, ref FileMaskSecondByte fb1, ref FileMaskThirdByte fb2,
                        ref FileMaskFourthByte fb3, ref FileMaskFifthByte fb4, ref FileAnimeMaskFirstByte ab0,
                        ref FileAnimeMaskSecondByte ab1, ref FileAnimeMaskThirdByte ab2,
                        ref FileAnimeMaskFourthByte ab3) => ab3 |= FileAnimeMaskFourthByte.GroupShortName
                },
                {
                    "resolution",
                    (ref FileMaskFirstByte fb0, ref FileMaskSecondByte fb1, ref FileMaskThirdByte fb2,
                        ref FileMaskFourthByte fb3, ref FileMaskFifthByte fb4, ref FileAnimeMaskFirstByte ab0,
                        ref FileAnimeMaskSecondByte ab1, ref FileAnimeMaskThirdByte ab2,
                        ref FileAnimeMaskFourthByte ab3) => fb2 |= FileMaskThirdByte.VideoResolution
                },
                {
                    "videoCodec",
                    (ref FileMaskFirstByte fb0, ref FileMaskSecondByte fb1, ref FileMaskThirdByte fb2,
                        ref FileMaskFourthByte fb3, ref FileMaskFifthByte fb4, ref FileAnimeMaskFirstByte ab0,
                        ref FileAnimeMaskSecondByte ab1, ref FileAnimeMaskThirdByte ab2,
                        ref FileAnimeMaskFourthByte ab3) => fb2 |= FileMaskThirdByte.VideoCodec
                },
                {
                    "crc32",
                    (ref FileMaskFirstByte fb0, ref FileMaskSecondByte fb1, ref FileMaskThirdByte fb2,
                        ref FileMaskFourthByte fb3, ref FileMaskFifthByte fb4, ref FileAnimeMaskFirstByte ab0,
                        ref FileAnimeMaskSecondByte ab1, ref FileAnimeMaskThirdByte ab2,
                        ref FileAnimeMaskFourthByte ab3) => fb1 |= FileMaskSecondByte.Crc32
                },
                {
                    "ed2k",
                    (ref FileMaskFirstByte fb0, ref FileMaskSecondByte fb1, ref FileMaskThirdByte fb2,
                        ref FileMaskFourthByte fb3, ref FileMaskFifthByte fb4, ref FileAnimeMaskFirstByte ab0,
                        ref FileAnimeMaskSecondByte ab1, ref FileAnimeMaskThirdByte ab2,
                        ref FileAnimeMaskFourthByte ab3) => fb1 |= FileMaskSecondByte.Ed2k
                },
                {
                    "md5",
                    (ref FileMaskFirstByte fb0, ref FileMaskSecondByte fb1, ref FileMaskThirdByte fb2,
                        ref FileMaskFourthByte fb3, ref FileMaskFifthByte fb4, ref FileAnimeMaskFirstByte ab0,
                        ref FileAnimeMaskSecondByte ab1, ref FileAnimeMaskThirdByte ab2,
                        ref FileAnimeMaskFourthByte ab3) => fb1 |= FileMaskSecondByte.Md5
                },
                {
                    "sha1",
                    (ref FileMaskFirstByte fb0, ref FileMaskSecondByte fb1, ref FileMaskThirdByte fb2,
                        ref FileMaskFourthByte fb3, ref FileMaskFifthByte fb4, ref FileAnimeMaskFirstByte ab0,
                        ref FileAnimeMaskSecondByte ab1, ref FileAnimeMaskThirdByte ab2,
                        ref FileAnimeMaskFourthByte ab3) => fb1 |= FileMaskSecondByte.Sha1
                },
            };

        internal static (IReadOnlyList<IFileFormatEmitter> Emitters, FileMask FileMask, FileAnimeMask AnimeMask) Parse(
            [NotNull] string format)
        {
            if (string.IsNullOrWhiteSpace(format))
            {
                throw new ArgumentNullException(nameof(format));
            }

            FileMaskFirstByte fb0 = 0;
            FileMaskSecondByte fb1 = 0;
            FileMaskThirdByte fb2 = 0;
            FileMaskFourthByte fb3 = 0;
            FileMaskFifthByte fb4 = 0;
            FileAnimeMaskFirstByte ab0 = 0;
            FileAnimeMaskSecondByte ab1 = 0;
            FileAnimeMaskThirdByte ab2 = 0;
            FileAnimeMaskFourthByte ab3 = 0;

            var mode = FileFormatParserMode.Constant;

            var emitters = new List<IFileFormatEmitter>();

            bool ellipsize = false;
            var ellipsizeBuffer = new StringBuilder(3);

            var buffer = new StringBuilder();

            var prefixBuffer = new StringBuilder();
            var suffixBuffer = new StringBuilder();

            char lastChar = format[0];

            for (int idx = 0; idx < format.Length; idx++)
            {
                char c = format[idx];

                switch (mode)
                {
                    case FileFormatParserMode.Constant:
                        if (c == '{')
                        {
                            emitters.Add(new ConstFileFormatEmitter(buffer.ToString()));
                            buffer.Clear();
                            mode = FileFormatParserMode.VariableStart;
                        }
                        else
                        {
                            buffer.Append(c);
                        }

                        break;
                    case FileFormatParserMode.VariableStart:
                        if (c == '\'')
                        {
                            mode = FileFormatParserMode.VariablePrefix;
                        }
                        else
                        {
                            buffer.Append(c);
                            mode = FileFormatParserMode.Variable;
                        }

                        break;
                    case FileFormatParserMode.Variable:
                        if (c == '}')
                        {
                            string variableName = buffer.ToString();
                            buffer.Clear();
                            string prefix = prefixBuffer.ToString();
                            prefixBuffer.Clear();
                            string suffix = suffixBuffer.ToString();
                            suffixBuffer.Clear();
                            mode = FileFormatParserMode.Constant;

                            if (!VariableFormatters.ContainsKey(variableName) ||
                                !FlagModifiers.ContainsKey(variableName))
                            {
                                throw new InvalidFormatPathException($"Unknown variable: {variableName}");
                            }

                            if (ellipsizeBuffer.Length == 3)
                            {
                                ellipsize = true;
                            }
                            ellipsizeBuffer.Clear();

                            emitters.Add(VariableFormatters[variableName](prefix, suffix, ellipsize));
                            FlagModifiers[variableName](ref fb0, ref fb1, ref fb2, ref fb3, ref fb4, ref ab0, ref ab1,
                                ref ab2, ref ab3);
                            ellipsize = false;
                        }
                        else if (c == '{' || c == '}')
                        {
                            if ((lastChar == '{' || lastChar == '}') && c != lastChar)
                            {
                                throw new InvalidFormatPathException(
                                    $"Cannot have mixed curly brace escape sequence at column {idx}: {lastChar}{c}");
                            }

                            mode = FileFormatParserMode.Constant;
                        }
                        else if (c == '\'')
                        {
                            mode = FileFormatParserMode.VariableSuffix;
                        }
                        else if (c == '.' && ellipsizeBuffer.Length < 3)
                        {
                            ellipsizeBuffer.Append(c);
                        }
                        else
                        {
                            buffer.Append(c);
                        }

                        break;
                    case FileFormatParserMode.VariableEnd:
                        if (c == '}')
                        {
                            string variableName = buffer.ToString();
                            buffer.Clear();
                            string prefix = prefixBuffer.ToString();
                            prefixBuffer.Clear();
                            string suffix = suffixBuffer.ToString();
                            suffixBuffer.Clear();
                            mode = FileFormatParserMode.Constant;

                            if (!VariableFormatters.ContainsKey(variableName) ||
                                !FlagModifiers.ContainsKey(variableName))
                            {
                                throw new InvalidFormatPathException($"Unknown variable: {variableName}");
                            }

                            if (ellipsizeBuffer.Length == 3)
                            {
                                ellipsize = true;
                            }
                            ellipsizeBuffer.Clear();

                            emitters.Add(VariableFormatters[variableName](prefix, suffix, ellipsize));
                            FlagModifiers[variableName](ref fb0, ref fb1, ref fb2, ref fb3, ref fb4, ref ab0, ref ab1,
                                ref ab2, ref ab3);
                            ellipsize = false;
                        }
                        else if (c == '.' && ellipsizeBuffer.Length < 3)
                        {
                            ellipsizeBuffer.Append(c);
                        }
                        else
                        {
                            throw new InvalidFormatPathException(
                                $"Invalid character '{c}' after variable suffix at column {idx}");
                        }

                        break;
                    case FileFormatParserMode.VariablePrefix:
                        if (c == '\'')
                        {
                            mode = FileFormatParserMode.Variable;
                        }
                        else
                        {
                            prefixBuffer.Append(c);
                        }

                        break;
                    case FileFormatParserMode.VariableSuffix:
                        if (c == '\'')
                        {
                            mode = FileFormatParserMode.VariableEnd;
                        }
                        else
                        {
                            suffixBuffer.Append(c);
                        }

                        break;
                }

                lastChar = c;
            }

            if (mode == FileFormatParserMode.Constant && buffer.Length > 0)
            {
                emitters.Add(new ConstFileFormatEmitter(buffer.ToString()));
            }

            return (emitters, new FileMask(fb0, fb1, fb2, fb3, fb4), new FileAnimeMask(ab0, ab1, ab2, ab3));
        }
    }

    enum FileFormatParserMode
    {
        Constant,

        Variable,

        VariableStart,

        VariableEnd,

        VariablePrefix,

        VariableSuffix,

        Ellipsize
    }
}