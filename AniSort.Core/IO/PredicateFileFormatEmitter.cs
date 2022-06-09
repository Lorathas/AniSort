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
using AniDbSharp.Data;
using AniSort.Core.Data;

namespace AniSort.Core.IO
{
    /// <summary>
    /// Variable emitter
    /// </summary>
    class PredicateFileFormatEmitter : IFileFormatEmitter
    {
        private readonly Func<FileInfo, FileAnimeInfo, Dictionary<string, string>, string> infoPredicate;

        private readonly Func<LocalFile, Dictionary<string, string>, string> localFilePredicate;

        private readonly string prefix;

        private readonly string suffix;

        public bool Ellipsize { get; }

        public PredicateFileFormatEmitter(Func<FileInfo, FileAnimeInfo, Dictionary<string, string>, string> infoPredicate, Func<LocalFile, Dictionary<string, string>, string> localFilePredicate, string prefix = null, string suffix = null, bool ellipsize = false)
        {
            this.infoPredicate = infoPredicate;
            this.prefix = prefix ?? string.Empty;
            this.suffix = suffix ?? string.Empty;
            Ellipsize = ellipsize;
            this.localFilePredicate = localFilePredicate;
        }

        /// <inheritdoc />
        public string Emit(FileInfo fileInfo, FileAnimeInfo animeInfo, Dictionary<string, string> overrides)
        {
            string predicateResult = infoPredicate(fileInfo, animeInfo, overrides);

            if (!string.IsNullOrWhiteSpace(predicateResult))
            {
                return $"{prefix}{predicateResult}{suffix}";
            }
            else
            {
                return string.Empty;
            }
        }

        /// <inheritdoc />
        public string Emit(LocalFile file, Dictionary<string, string> overrides)
        {
            string predicateResult = localFilePredicate(file, overrides);

            if (!string.IsNullOrWhiteSpace(predicateResult))
            {
                return $"{prefix}{predicateResult}{suffix}";
            }
            else
            {
                return string.Empty;
            }
        }

        public string Emit(FileInfo fileInfo, FileAnimeInfo animeInfo, Dictionary<string, string> overrides, int trimLength)
        {
            string predicateResult = infoPredicate(fileInfo, animeInfo, overrides);

            if (!string.IsNullOrWhiteSpace(predicateResult))
            {
                string built = $"{prefix}{predicateResult}{suffix}";

                if (built.Length - trimLength < 0)
                {
                    return string.Empty;
                }
                
                return built.Substring(0, built.Length - trimLength);
            }
            else
            {
                return string.Empty;
            }
        }
        
        

        public string Emit(LocalFile file, Dictionary<string, string> overrides, int trimLength)
        {
            string predicateResult = localFilePredicate(file, overrides);

            if (!string.IsNullOrWhiteSpace(predicateResult))
            {
                string built = $"{prefix}{predicateResult}{suffix}";

                if (built.Length - trimLength < 0)
                {
                    return string.Empty;
                }
                
                return built.Substring(0, built.Length - trimLength);
            }
            else
            {
                return string.Empty;
            }
        }
    }
}
