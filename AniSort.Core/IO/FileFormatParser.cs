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
                = new()
                {
                    {
                        "animeEnglish",
                        (prefix, suffix, ellipsize) =>
                            new PredicateFileFormatEmitter((_, a, _) => a.EnglishName, (f, _) => f.EpisodeFile.Episode.Anime.EnglishName, prefix, suffix, ellipsize)
                    },
                    {
                        "animeRomaji",
                        (prefix, suffix, ellipsize) =>
                            new PredicateFileFormatEmitter((_, a, _) => a.RomajiName, (f, _) => f.EpisodeFile.Episode.Anime.RomajiName, prefix, suffix, ellipsize)
                    },
                    {
                        "animeKanji",
                        (prefix, suffix, ellipsize) =>
                            new PredicateFileFormatEmitter((_, a, _) => a.KanjiName, (f, _) => f.EpisodeFile.Episode.Anime.KanjiName, prefix, suffix, ellipsize)
                    },
                    {
                        "episodeNumber",
                        (prefix, suffix, ellipsize) => new PredicateFileFormatEmitter((_, a, _) =>
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
                        },
                            (f, _) =>
                            {
                                int paddingDigits = 6;
                                for (int idx = 2; idx < 6; idx++)
                                {
                                    if (Math.Pow(10, idx) > f.EpisodeFile.Episode.Anime.HighestEpisodeNumber)
                                    {
                                        paddingDigits = idx;
                                        break;
                                    }
                                }

                                if (f.EpisodeFile.Episode.Number.Length < paddingDigits)
                                {
                                    var builder = new StringBuilder();

                                    for (int idx = 0; idx < paddingDigits - f.EpisodeFile.Episode.Number.Length; idx++)
                                    {
                                        builder.Append('0');
                                    }

                                    builder.Append(f.EpisodeFile.Episode.Number);

                                    return builder.ToString();
                                }
                                else
                                {
                                    return f.EpisodeFile.Episode.Number;
                                }
                            }, prefix, suffix, ellipsize)
                    },
                    {
                        "fileVersion",
                        (prefix, suffix, ellipsize) => new PredicateFileFormatEmitter((f, _, _) =>
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
                        },
                            (f, _) =>
                        {
                            if (f.EpisodeFile.State.HasFlag(FileState.IsVersion5))
                            {
                                return "5";
                            }
                            else if (f.EpisodeFile.State.HasFlag(FileState.IsVersion4))
                            {
                                return "4";
                            }
                            else if (f.EpisodeFile.State.HasFlag(FileState.IsVersion3))
                            {
                                return "3";
                            }
                            else if (f.EpisodeFile.State.HasFlag(FileState.IsVersion2))
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
                            new PredicateFileFormatEmitter((_, a, _) => a.EpisodeName, (f, _) => f.EpisodeFile.Episode.EnglishName, prefix, suffix, ellipsize)
                    },
                    {
                        "episodeRomaji",
                        (prefix, suffix, ellipsize) =>
                            new PredicateFileFormatEmitter((_, a, _) => a.EpisodeRomajiName, (f, _) => f.EpisodeFile.Episode.RomajiName, prefix, suffix, ellipsize)
                    },
                    {
                        "episodeKanji",
                        (prefix, suffix, ellipsize) =>
                            new PredicateFileFormatEmitter((_, a, _) => a.EpisodeKanjiName, (f, _) => f.EpisodeFile.Episode.KanjiName, prefix, suffix, ellipsize)
                    },
                    {
                        "group",
                        (prefix, suffix, ellipsize) =>
                            new PredicateFileFormatEmitter((_, a, _) => a.GroupName, (f, _) => f.EpisodeFile.Group.Name, prefix, suffix, ellipsize)
                    },
                    {
                        "groupShort",
                        (prefix, suffix, ellipsize) =>
                            new PredicateFileFormatEmitter((_, a, _) => a.GroupShortName, (f, _) => f.EpisodeFile.Group.ShortName, prefix, suffix, ellipsize)
                    },
                    {
                        "resolution",
                        (prefix, suffix, ellipsize) =>
                            new PredicateFileFormatEmitter((f, _, r) => r.TryGetValue("resolution", out var res) ? res : f.VideoResolution, (f, _) => $"{f.EpisodeFile.Resolution.Width}x{f.EpisodeFile.Resolution.Height}", prefix, suffix, ellipsize)
                    },
                    {
                        "videoCodec",
                        (prefix, suffix, ellipsize) =>
                            new PredicateFileFormatEmitter((f, _, _) => f.VideoCodec, (f, _) => f.EpisodeFile.VideoCodec, prefix, suffix, ellipsize)
                    },
                    {
                        "crc32",
                        (prefix, suffix, ellipsize) =>
                            new PredicateFileFormatEmitter((f, _, _) => f.Crc32Hash.ToHexString(), (f, _) => f.EpisodeFile.Crc32Hash.ToHexString(), prefix, suffix,
                                ellipsize)
                    },
                    {
                        "ed2k",
                        (prefix, suffix, ellipsize) =>
                            new PredicateFileFormatEmitter((f, _, _) => f.Ed2kHash.ToHexString(), (f, _) => f.Ed2kHash.ToHexString(), prefix, suffix,
                                ellipsize)
                    },
                    {
                        "md5",
                        (prefix, suffix, ellipsize) => new PredicateFileFormatEmitter((f, _, _) => f.Md5Hash.ToHexString(),
                            (f, _) => f.EpisodeFile.Md5Hash.ToHexString(), prefix, suffix, ellipsize)
                    },
                    {
                        "sha1",
                        (prefix, suffix, ellipsize) =>
                            new PredicateFileFormatEmitter((f, _, _) => f.Sha1Hash.ToHexString(), (f, _) => f.EpisodeFile.Sha1Hash.ToHexString(), prefix, suffix,
                                ellipsize)
                    },
                };

        private static readonly Dictionary<string, FlagModifier> FlagModifiers =
            new()
            {
                {
                    "animeEnglish",
                    (ref FileMaskFirstByte _, ref FileMaskSecondByte _, ref FileMaskThirdByte _,
                        ref FileMaskFourthByte _, ref FileMaskFifthByte _, ref FileAnimeMaskFirstByte _,
                        ref FileAnimeMaskSecondByte ab1, ref FileAnimeMaskThirdByte _,
                        ref FileAnimeMaskFourthByte _) => ab1 |= FileAnimeMaskSecondByte.EnglishName
                },
                {
                    "animeRomaji",
                    (ref FileMaskFirstByte _, ref FileMaskSecondByte _, ref FileMaskThirdByte _,
                        ref FileMaskFourthByte _, ref FileMaskFifthByte _, ref FileAnimeMaskFirstByte _,
                        ref FileAnimeMaskSecondByte ab1, ref FileAnimeMaskThirdByte _,
                        ref FileAnimeMaskFourthByte _) => ab1 |= FileAnimeMaskSecondByte.RomajiName
                },
                {
                    "animeKanji",
                    (ref FileMaskFirstByte _, ref FileMaskSecondByte _, ref FileMaskThirdByte _,
                        ref FileMaskFourthByte _, ref FileMaskFifthByte _, ref FileAnimeMaskFirstByte _,
                        ref FileAnimeMaskSecondByte ab1, ref FileAnimeMaskThirdByte _,
                        ref FileAnimeMaskFourthByte _) => ab1 |= FileAnimeMaskSecondByte.KanjiName
                },
                {
                    "episodeNumber",
                    (ref FileMaskFirstByte _, ref FileMaskSecondByte _, ref FileMaskThirdByte _,
                        ref FileMaskFourthByte _, ref FileMaskFifthByte _, ref FileAnimeMaskFirstByte ab0,
                        ref FileAnimeMaskSecondByte _, ref FileAnimeMaskThirdByte ab2,
                        ref FileAnimeMaskFourthByte _) =>
                    {
                        ab2 |= FileAnimeMaskThirdByte.EpisodeNumber;
                        ab0 |= FileAnimeMaskFirstByte.TotalEpisodes | FileAnimeMaskFirstByte.HighestEpisodeNumber;
                    }
                },
                {
                    "fileVersion",
                    (ref FileMaskFirstByte fb0, ref FileMaskSecondByte _, ref FileMaskThirdByte _,
                        ref FileMaskFourthByte _, ref FileMaskFifthByte _, ref FileAnimeMaskFirstByte _,
                        ref FileAnimeMaskSecondByte _, ref FileAnimeMaskThirdByte _,
                        ref FileAnimeMaskFourthByte _) => fb0 |= FileMaskFirstByte.State
                },
                {
                    "episodeEnglish",
                    (ref FileMaskFirstByte _, ref FileMaskSecondByte _, ref FileMaskThirdByte _,
                        ref FileMaskFourthByte _, ref FileMaskFifthByte _, ref FileAnimeMaskFirstByte _,
                        ref FileAnimeMaskSecondByte _, ref FileAnimeMaskThirdByte ab2,
                        ref FileAnimeMaskFourthByte _) => ab2 |= FileAnimeMaskThirdByte.EpisodeName
                },
                {
                    "episodeRomaji",
                    (ref FileMaskFirstByte _, ref FileMaskSecondByte _, ref FileMaskThirdByte _,
                        ref FileMaskFourthByte _, ref FileMaskFifthByte _, ref FileAnimeMaskFirstByte _,
                        ref FileAnimeMaskSecondByte _, ref FileAnimeMaskThirdByte ab2,
                        ref FileAnimeMaskFourthByte _) => ab2 |= FileAnimeMaskThirdByte.EpisodeRomajiName
                },
                {
                    "episodeKanji",
                    (ref FileMaskFirstByte _, ref FileMaskSecondByte _, ref FileMaskThirdByte _,
                        ref FileMaskFourthByte _, ref FileMaskFifthByte _, ref FileAnimeMaskFirstByte _,
                        ref FileAnimeMaskSecondByte _, ref FileAnimeMaskThirdByte ab2,
                        ref FileAnimeMaskFourthByte _) => ab2 |= FileAnimeMaskThirdByte.EpisodeKanjiName
                },
                {
                    "group",
                    (ref FileMaskFirstByte _, ref FileMaskSecondByte _, ref FileMaskThirdByte _,
                        ref FileMaskFourthByte _, ref FileMaskFifthByte _, ref FileAnimeMaskFirstByte _,
                        ref FileAnimeMaskSecondByte _, ref FileAnimeMaskThirdByte _,
                        ref FileAnimeMaskFourthByte ab3) => ab3 |= FileAnimeMaskFourthByte.GroupName
                },
                {
                    "groupShort",
                    (ref FileMaskFirstByte _, ref FileMaskSecondByte _, ref FileMaskThirdByte _,
                        ref FileMaskFourthByte _, ref FileMaskFifthByte _, ref FileAnimeMaskFirstByte _,
                        ref FileAnimeMaskSecondByte _, ref FileAnimeMaskThirdByte _,
                        ref FileAnimeMaskFourthByte ab3) => ab3 |= FileAnimeMaskFourthByte.GroupShortName
                },
                {
                    "resolution",
                    (ref FileMaskFirstByte _, ref FileMaskSecondByte _, ref FileMaskThirdByte fb2,
                        ref FileMaskFourthByte _, ref FileMaskFifthByte _, ref FileAnimeMaskFirstByte _,
                        ref FileAnimeMaskSecondByte _, ref FileAnimeMaskThirdByte _,
                        ref FileAnimeMaskFourthByte _) => fb2 |= FileMaskThirdByte.VideoResolution
                },
                {
                    "videoCodec",
                    (ref FileMaskFirstByte _, ref FileMaskSecondByte _, ref FileMaskThirdByte fb2,
                        ref FileMaskFourthByte _, ref FileMaskFifthByte _, ref FileAnimeMaskFirstByte _,
                        ref FileAnimeMaskSecondByte _, ref FileAnimeMaskThirdByte _,
                        ref FileAnimeMaskFourthByte _) => fb2 |= FileMaskThirdByte.VideoCodec
                },
                {
                    "crc32",
                    (ref FileMaskFirstByte _, ref FileMaskSecondByte fb1, ref FileMaskThirdByte _,
                        ref FileMaskFourthByte _, ref FileMaskFifthByte _, ref FileAnimeMaskFirstByte _,
                        ref FileAnimeMaskSecondByte _, ref FileAnimeMaskThirdByte _,
                        ref FileAnimeMaskFourthByte _) => fb1 |= FileMaskSecondByte.Crc32
                },
                {
                    "ed2k",
                    (ref FileMaskFirstByte _, ref FileMaskSecondByte fb1, ref FileMaskThirdByte _,
                        ref FileMaskFourthByte _, ref FileMaskFifthByte _, ref FileAnimeMaskFirstByte _,
                        ref FileAnimeMaskSecondByte _, ref FileAnimeMaskThirdByte _,
                        ref FileAnimeMaskFourthByte _) => fb1 |= FileMaskSecondByte.Ed2k
                },
                {
                    "md5",
                    (ref FileMaskFirstByte _, ref FileMaskSecondByte fb1, ref FileMaskThirdByte _,
                        ref FileMaskFourthByte _, ref FileMaskFifthByte _, ref FileAnimeMaskFirstByte _,
                        ref FileAnimeMaskSecondByte _, ref FileAnimeMaskThirdByte _,
                        ref FileAnimeMaskFourthByte _) => fb1 |= FileMaskSecondByte.Md5
                },
                {
                    "sha1",
                    (ref FileMaskFirstByte _, ref FileMaskSecondByte fb1, ref FileMaskThirdByte _,
                        ref FileMaskFourthByte _, ref FileMaskFifthByte _, ref FileAnimeMaskFirstByte _,
                        ref FileAnimeMaskSecondByte _, ref FileAnimeMaskThirdByte _,
                        ref FileAnimeMaskFourthByte _) => fb1 |= FileMaskSecondByte.Sha1
                },
            };

        internal static (IReadOnlyList<IFileFormatEmitter> Emitters, FileMask FileMask, FileAnimeMask AnimeMask) Parse(
            [NotNull] string format,
            FileMask fileMaskOverrides = null,
            FileAnimeMask animeMaskOverrides = null)
        {
            if (string.IsNullOrWhiteSpace(format))
            {
                throw new ArgumentNullException(nameof(format));
            }

            FileMaskFirstByte fb0 = fileMaskOverrides?.FirstByteFlags ?? 0;
            FileMaskSecondByte fb1 = fileMaskOverrides?.SecondByteFlags ?? 0;
            FileMaskThirdByte fb2 = fileMaskOverrides?.ThirdByteFlags ?? 0;
            FileMaskFourthByte fb3 = fileMaskOverrides?.FourthByteFlags ?? 0;
            FileMaskFifthByte fb4 = fileMaskOverrides?.FifthByteFlags ?? 0;
            FileAnimeMaskFirstByte ab0 = animeMaskOverrides?.FirstByteFlags ?? 0;
            FileAnimeMaskSecondByte ab1 = animeMaskOverrides?.SecondByteFlags ?? 0;
            FileAnimeMaskThirdByte ab2 = animeMaskOverrides?.ThirdByteFlags ?? 0;
            FileAnimeMaskFourthByte ab3 = animeMaskOverrides?.FourthByteFlags ?? 0;

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
