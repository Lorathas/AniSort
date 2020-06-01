﻿// Copyright © 2020 Lorathas
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
using AniDbSharp.Data;

namespace AniSort.Core.IO
{
    /// <summary>
    /// Variable emitter
    /// </summary>
    class PredicateFileFormatEmitter : IFileFormatEmitter
    {
        private readonly Func<FileInfo, FileAnimeInfo, string> predicate;

        private readonly string prefix;

        private readonly string suffix;

        public PredicateFileFormatEmitter(Func<FileInfo, FileAnimeInfo, string> predicate, string prefix = null, string suffix = null)
        {
            this.predicate = predicate;
            this.prefix = prefix ?? string.Empty;
            this.suffix = suffix ?? string.Empty;
        }

        /// <inheritdoc />
        public string Emit(FileInfo fileInfo, FileAnimeInfo animeInfo)
        {
            string predicateResult = predicate(fileInfo, animeInfo);

            if (!string.IsNullOrWhiteSpace(predicateResult))
            {
                return $"{prefix}{predicateResult}{suffix}";
            }
            else
            {
                return string.Empty;
            }
        }
    }
}
