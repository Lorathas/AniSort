// // Copyright © 2022 Lorathas
// //
// // Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation
// // files (the “Software”), to deal in the Software without restriction, including without limitation the rights to use, copy,
// // modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the
// // Software is furnished to do so, subject to the following conditions:
// //
// // The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
// //
// // THE SOFTWARE IS PROVIDED “AS IS”, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
// // OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// // LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR
// // IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using AniDbSharp.Data;

namespace AniSort.Core.IO;

public class PathBuilderRepository : IPathBuilderRepository
{
    private readonly Dictionary<string, PathBuilder> libraryPathBuilders = new();

    public PathBuilderRepository(IConfigProvider configProvider)
    {
        if (configProvider.Config.LibraryPaths != null)
        {
            foreach (string path in configProvider.Config.LibraryPaths)
            {
                libraryPathBuilders[path] = PathBuilder.Compile(
                    path,
                    configProvider.Config.Destination.TvPath,
                    configProvider.Config.Destination.MoviePath,
                    configProvider.Config.Destination.Format,
                    new FileMask { FirstByteFlags = FileMaskFirstByte.AnimeId | FileMaskFirstByte.GroupId | FileMaskFirstByte.EpisodeId, SecondByteFlags = FileMaskSecondByte.Ed2k });
            }
        }


        if (string.IsNullOrWhiteSpace(configProvider.Config.Destination.Path))
        {
            DefaultPathBuilder = libraryPathBuilders.Values.FirstOrDefault();
        }
        else
        {
            DefaultPathBuilder = PathBuilder.Compile(
                configProvider.Config.Destination.Path,
                configProvider.Config.Destination.TvPath,
                configProvider.Config.Destination.MoviePath,
                configProvider.Config.Destination.Format,
                new FileMask { FirstByteFlags = FileMaskFirstByte.AnimeId | FileMaskFirstByte.GroupId | FileMaskFirstByte.EpisodeId, SecondByteFlags = FileMaskSecondByte.Ed2k });
        }
    }

    /// <inheritdoc />
    public PathBuilder DefaultPathBuilder { get; }

    /// <inheritdoc />
    public PathBuilder GetPathBuilderForPath(string path)
    {
        foreach (var (key, builder) in libraryPathBuilders)
        {
            if (path.ToLowerInvariant().StartsWith(key.ToLowerInvariant()))
            {
                return builder;
            }
        }

        return DefaultPathBuilder;
    }
}
