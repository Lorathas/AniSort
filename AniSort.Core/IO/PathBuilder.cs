﻿// Copyright © 2022 Lorathas
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
using System.IO;
using System.Linq;
using System.Text;
using AniDbSharp.Data;
using AniSort.Core.Data;
using AniSort.Core.Exceptions;
using AniSort.Core.Models;
using FileInfo = AniDbSharp.Data.FileInfo;

namespace AniSort.Core.IO
{
    public class PathBuilder
    {
        public string Root { get; }

        private readonly AnimeTypeFileFormatEmitter animeTypeEmitter;

        private readonly IReadOnlyList<IFileFormatEmitter> emitters;

        public FileMask FileMask { get; }

        public FileAnimeMask AnimeMask { get; }

        private PathBuilder([NotNull] string root, [NotNull] AnimeTypeFileFormatEmitter animeTypeEmitter,
            [NotNull] IReadOnlyList<IFileFormatEmitter> emitters, [NotNull] FileMask fileMask,
            [NotNull] FileAnimeMask animeMask)
        {
            if (emitters == null)
            {
                throw new ArgumentNullException(nameof(emitters));
            }

            if (emitters.Count == 0)
            {
                throw new ArgumentException($"{nameof(emitters)} must have at least one emitter");
            }

            if (string.IsNullOrWhiteSpace(root))
            {
                throw new ArgumentNullException(nameof(root));
            }

            this.Root = root;
            this.animeTypeEmitter = animeTypeEmitter ?? throw new ArgumentNullException(nameof(animeTypeEmitter));
            this.emitters = emitters;
            FileMask = fileMask ?? throw new ArgumentNullException(nameof(fileMask));
            AnimeMask = animeMask ?? throw new ArgumentNullException(nameof(animeMask));
        }

        public string BuildPath(FileInfo fileInfo, FileAnimeInfo animeInfo, int maxLength, VideoResolution resolution = null)
        {
            var overrides = new Dictionary<string, string>();

            if (resolution != null)
            {
                overrides["resolution"] = $"{resolution.Width}x{resolution.Height}";
            }
            
            var builder = new StringBuilder();

            foreach (var emitter in emitters)
            {
                builder.Append(emitter.Emit(fileInfo, animeInfo, overrides).CleanPath());
            }

            string cleanedRoot = Root.CleanRootPath();
            string cleanedAnimePath = animeTypeEmitter.Emit(fileInfo, animeInfo, overrides).CleanPath();

            string path = Path.Combine(cleanedRoot, cleanedAnimePath, builder.ToString());

            if (path.Length > maxLength)
            {
                builder.Clear();
                int overExtension = path.Length - maxLength;

                for (int idx = emitters.Count - 1; idx >= 0; idx--)
                {
                    var emitter = emitters[idx];

                    if (emitter is PredicateFileFormatEmitter { Ellipsize: true } predicateEmitter)
                    {
                        string emitted = predicateEmitter.Emit(fileInfo, animeInfo, overrides, overExtension + 3).CleanPath() + "...";

                        builder.Append(new string(emitted.Reverse().ToArray()));
                    }
                    else
                    {
                        builder.Append(new string(emitter.Emit(fileInfo, animeInfo, overrides).CleanPath().Reverse().ToArray()));
                    }
                }

                string built = new string(builder.ToString().Reverse().ToArray());

                path = Path.Combine(cleanedRoot, cleanedAnimePath, built);
            }

            return path;
        }
        
        public string BuildPath(LocalFile file, int maxLength, VideoResolution resolution = null)
        {
            var overrides = new Dictionary<string, string>();

            if (resolution != null)
            {
                overrides["resolution"] = $"{resolution.Width}x{resolution.Height}";
            }
            
            var builder = new StringBuilder();

            foreach (var emitter in emitters)
            {
                builder.Append(emitter.Emit(file, overrides).CleanPath());
            }

            string cleanedRoot = Root.CleanRootPath();
            string cleanedAnimePath = animeTypeEmitter.Emit(file, overrides).CleanPath();

            string path = Path.Combine(cleanedRoot, cleanedAnimePath, builder.ToString());

            if (path.Length > maxLength)
            {
                builder.Clear();
                int overExtension = path.Length - maxLength;

                for (int idx = emitters.Count - 1; idx >= 0; idx--)
                {
                    var emitter = emitters[idx];

                    if (emitter is PredicateFileFormatEmitter { Ellipsize: true } predicateEmitter)
                    {
                        string emitted = predicateEmitter.Emit(file, overrides, overExtension + 3).CleanPath() + "...";

                        builder.Append(new string(emitted.Reverse().ToArray()));
                    }
                    else
                    {
                        builder.Append(new string(emitter.Emit(file, overrides).CleanPath().Reverse().ToArray()));
                    }
                }

                string built = new string(builder.ToString().Reverse().ToArray());

                path = Path.Combine(cleanedRoot, cleanedAnimePath, built);
            }

            return path;
        }

        public static PathBuilder Compile([NotNull] string root, [NotNull] string tvPath, [NotNull] string moviePath,
            [NotNull] string format, FileMask fileMaskOverrides = null, FileAnimeMask animeMaskOverrides = null, Dictionary<string, string> overrides = null)
        {
            if (string.IsNullOrWhiteSpace(root))
            {
                throw new ArgumentNullException(nameof(root));
            }

            if (string.IsNullOrWhiteSpace(tvPath))
            {
                throw new ArgumentNullException(nameof(tvPath));
            }

            if (string.IsNullOrWhiteSpace(moviePath))
            {
                throw new ArgumentNullException(nameof(moviePath));
            }

            if (string.IsNullOrWhiteSpace(format))
            {
                throw new ArgumentNullException(nameof(format));
            }

            var (emitters, fileMask, animeMask) = FileFormatParser.Parse(format, fileMaskOverrides, animeMaskOverrides);

            return new PathBuilder(root, new AnimeTypeFileFormatEmitter(tvPath, moviePath, overrides), emitters, fileMask,
                new FileAnimeMask(animeMask.FirstByteFlags | FileAnimeMaskFirstByte.Type, animeMask.SecondByteFlags,
                    animeMask.ThirdByteFlags, animeMask.FourthByteFlags));
        }
    }
}