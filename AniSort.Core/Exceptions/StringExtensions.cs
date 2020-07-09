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
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace AniSort.Core.Exceptions
{
    public static class StringExtensions
    {
        private static readonly string InvalidPathRegexString = $"[{new string(Path.GetInvalidPathChars())}\"!?*<>|:]+";
        private static readonly string InvalidRootPathRegexString = $"[{new string(Path.GetInvalidPathChars())}\"!?*<>|]+";
        private static readonly Regex InvalidPathRegex =
            new Regex(InvalidPathRegexString, RegexOptions.Compiled);
        private static readonly Regex InvalidRootPathRegex = new Regex(InvalidRootPathRegexString, RegexOptions.Compiled);
        private static readonly Regex WhitespaceRegex = new Regex("\\s{2,}", RegexOptions.Compiled);
        private static readonly Regex EllipsisRegex = new Regex("\\.{3}", RegexOptions.Compiled);

        private static readonly IReadOnlyList<Tuple<Regex, string>> RootCleaners = new List<Tuple<Regex, string>>
        {
            new Tuple<Regex, string>(WhitespaceRegex, " "),
            new Tuple<Regex, string>(InvalidRootPathRegex, "_")
        };

        private static readonly IReadOnlyList<Tuple<Regex, string>> PathCleaners = new List<Tuple<Regex, string>>
        {
            new Tuple<Regex, string>(WhitespaceRegex, " "),
            new Tuple<Regex, string>(InvalidPathRegex, "_"),
            new Tuple<Regex, string>(EllipsisRegex, "_")
        };

        public static string CleanPath(this string path)
        {
            string temp = path;

            foreach (var cleaner in PathCleaners)
            {
                temp = cleaner.Item1.Replace(temp, cleaner.Item2);
            }

            return temp;
        }

        public static string CleanRootPath(this string path)
        {
            string temp = path;

            foreach (var cleaner in RootCleaners)
            {
                temp = cleaner.Item1.Replace(temp, cleaner.Item2);
            }

            return temp;
        }
    }
}
