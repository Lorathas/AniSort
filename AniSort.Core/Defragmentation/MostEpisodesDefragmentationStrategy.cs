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
using System.Linq;
using AniSort.Core.Data;
using AniSort.Core.IO;

namespace AniSort.Core.Defragmentation;

/// <summary>
/// Defragmentation strategy that prefers the directory with the most files in it
/// </summary>
[DefragmentationStrategy("mostEpisodes")]
public class MostEpisodesDefragmentationStrategy : IDefragmentationStrategy
{
    private readonly IPathBuilderRepository pathBuilderRepository;

    public MostEpisodesDefragmentationStrategy(IPathBuilderRepository pathBuilderRepository)
    {
        this.pathBuilderRepository = pathBuilderRepository;
    }

    /// <inheritdoc />
    public IPathBuilder GetPathBuilderForFiles(IEnumerable<LocalFile> localFiles)
    {
        var enumerable = localFiles as LocalFile[] ?? localFiles?.ToArray();
        if (localFiles == null || enumerable.Length == 0)
        {
            return pathBuilderRepository.DefaultPathBuilder;
        }

        var counts = new Dictionary<IPathBuilder, int>();

        foreach (var file in enumerable)
        {
            var pathBuilder = pathBuilderRepository.GetPathBuilderForPath(file.Path);

            if (!counts.ContainsKey(pathBuilder))
            {
                counts[pathBuilder] = 0;
            }

            counts[pathBuilder]++;
        }

        return counts.MaxBy(p => p.Value).Key;
    }
}
