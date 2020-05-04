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

        private static readonly Dictionary<string, Func<string, string, PredicateFileFormatEmitter>> VariableFormatters
            = new Dictionary<string, Func<string, string, PredicateFileFormatEmitter>>
            {
                {
                    "animeEnglish",
                    (prefix, suffix) => new PredicateFileFormatEmitter((f, a) => a.EnglishName, prefix, suffix)
                },
                {
                    "animeRomaji",
                    (prefix, suffix) => new PredicateFileFormatEmitter((f, a) => a.RomajiName, prefix, suffix)
                },
                {
                    "animeKanji",
                    (prefix, suffix) => new PredicateFileFormatEmitter((f, a) => a.KanjiName, prefix, suffix)
                },
                {
                    "episodeNumber",
                    (prefix, suffix) => new PredicateFileFormatEmitter((f, a) => a.EpisodeNumber, prefix, suffix)
                },
                {
                    "fileVersion",
                    (prefix, suffix) => new PredicateFileFormatEmitter((f, a) => a.Type, prefix, suffix)
                },
                {
                    "episodeEnglish",
                    (prefix, suffix) => new PredicateFileFormatEmitter((f, a) => a.EpisodeName, prefix, suffix)
                },
                {
                    "episodeRomaji",
                    (prefix, suffix) => new PredicateFileFormatEmitter((f, a) => a.EpisodeRomajiName, prefix, suffix)
                },
                {
                    "episodeKanji",
                    (prefix, suffix) => new PredicateFileFormatEmitter((f, a) => a.EpisodeKanjiName, prefix, suffix)
                },
                {
                    "group",
                    (prefix, suffix) => new PredicateFileFormatEmitter((f, a) => a.GroupName, prefix, suffix)
                },
                {
                    "groupShort",
                    (prefix, suffix) => new PredicateFileFormatEmitter((f, a) => a.GroupShortName, prefix, suffix)
                },
                {
                    "resolution",
                    (prefix, suffix) => new PredicateFileFormatEmitter((f, a) => f.VideoResolution, prefix, suffix)
                },
                {
                    "videoCodec",
                    (prefix, suffix) => new PredicateFileFormatEmitter((f, a) => f.VideoCodec, prefix, suffix)
                },
                {
                    "crc32",
                    (prefix, suffix) => new PredicateFileFormatEmitter((f, a) => f.Crc32Hash.ToHexString(), prefix, suffix)
                },
                {
                    "sha1",
                    (prefix, suffix) => new PredicateFileFormatEmitter((f, a) => f.Sha1Hash.ToHexString(), prefix, suffix)
                },
                {
                    "ed2k",
                    (prefix, suffix) => new PredicateFileFormatEmitter((f, a) => f.Ed2kHash.ToHexString(), prefix, suffix)
                }
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
                }
            };

        internal static (IReadOnlyList<IFileFormatEmitter> Emitters, FileMask FileMask, FileAnimeMask AnimeMask) Parse(
            [NotNull] string format)
        {
            if (string.IsNullOrWhiteSpace(format))
            {
                throw new ArgumentNullException(nameof(format));
            }

            var mode = FileFormatParserMode.Constant;

            var emitters = new List<IFileFormatEmitter>();

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
                        if (c == '{' || c == '}')
                        {
                            if (c != lastChar)
                            {
                                throw new InvalidFormatPathException(
                                    $"Cannot have mixed curly brace escape sequence: {lastChar}{c}");
                            }

                            mode = FileFormatParserMode.Constant;
                        }
                        else if (c == '\'')
                        {
                            mode = FileFormatParserMode.VariableSuffix;
                        }
                        else if (c == '}')
                        {
                            string variableName = buffer.ToString();
                            buffer.Clear();
                            string prefix = prefixBuffer.ToString();
                            prefixBuffer.Clear();
                            string suffix = suffixBuffer.ToString();
                            suffixBuffer.Clear();
                            mode = FileFormatParserMode.Constant;

                            if (!VariableFormatters.ContainsKey(variableName))
                            {
                                throw new InvalidFormatPathException($"Unknown variable: {variableName}");
                            }
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

            return (emitters, new FileMask(fb0, fb1, fb2, fb3, fb4), new FileAnimeMask(ab0, ab1, ab3, ab4));
        }
    }

    enum FileFormatParserMode
    {
        Constant,

        Variable,

        VariableStart,

        VariableEnd,

        VariablePrefix,

        VariableSuffix
    }
}