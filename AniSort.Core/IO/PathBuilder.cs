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

namespace AniSort.Core.IO
{
    public class PathBuilder
    {
        private readonly IReadOnlyList<IFileFormatEmitter> emitters;
        public FileMask FileMask { get; }
        public FileAnimeMask AnimeMask { get; }

        private PathBuilder([NotNull] IReadOnlyList<IFileFormatEmitter> emitters, [NotNull] FileMask fileMask, [NotNull] FileAnimeMask animeMask)
        {
            if (emitters == null)
            {
                throw new ArgumentNullException(nameof(emitters));
            }

            if (emitters.Count == 0)
            {
                throw new ArgumentException($"{nameof(emitters)} must have at least one emitter");
            }

            this.emitters = emitters;
            FileMask = fileMask;
            AnimeMask = animeMask;
        }

        public string BuildPath()
        {
            var builder = new StringBuilder();

            return builder.ToString();
        }

        public static PathBuilder Compile(string root, string tvPath, string moviePath, string format)
        {
            var emitters = new List<IFileFormatEmitter>();

            emitters.Add(new ConstFileFormatEmitter(root));
            emitters.Add(new AnimeTypeFileFormatEmitter(tvPath, moviePath));

            return new PathBuilder(emitters);
        }
    }

    interface IFileFormatEmitter
    {
        string Emit(FileInfo fileInfo, FileAnimeInfo animeInfo);
    }

    /// <summary>
    /// Constant string emitter
    /// </summary>
    class ConstFileFormatEmitter : IFileFormatEmitter
    {
        private readonly string value;

        public ConstFileFormatEmitter(string value)
        {
            this.value = value;
        }

        /// <inheritdoc />
        public string Emit(FileInfo fileInfo, FileAnimeInfo animeInfo)
        {
            return value;
        }
    }

    /// <summary>
    /// Variable emitter
    /// </summary>
    class VariableFileFormatEmitter : IFileFormatEmitter
    {
        private readonly Func<FileInfo, FileAnimeInfo, string> predicate;

        public VariableFileFormatEmitter(Func<FileInfo, FileAnimeInfo, string> predicate)
        {
            this.predicate = predicate;
        }

        /// <inheritdoc />
        public string Emit(FileInfo fileInfo, FileAnimeInfo animeInfo)
        {
            return predicate(fileInfo, animeInfo);
        }
    }

    class AnimeFileFormatEmitter : IFileFormatEmitter
    {
        private readonly string tvPath;

        private readonly string moviePath;

        public AnimeFileFormatEmitter(string tvPath, string moviePath)
        {
            this.tvPath = tvPath;
            this.moviePath = moviePath;
        }

        /// <inheritdoc />
        public string Emit(FileInfo fileInfo, FileAnimeInfo animeInfo)
        {
            if (string.Equals(animeInfo.Type, "TV Series"))
            {
                return tvPath;
            }
            else
            {
                return moviePath;
            }
        }
    }
}
