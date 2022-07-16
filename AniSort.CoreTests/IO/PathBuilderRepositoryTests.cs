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
using AniSort.Core;
using AniSort.Core.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AniSort.CoreTests.IO;

[TestClass]
public class PathBuilderRepositoryTests
{
    private Config config;
    private PathBuilderRepository pathBuilderRepository;
    
    [ClassInitialize]
    public void Setup()
    {
        config = new Config
        {
            LibraryPaths = new List<string>
            {
                @"F:\s\a",
                @"G:\s\a",
                @"H:\s\a"
            },
            Destination = new DestinationConfig
            {
                Path = @"F:\s\a",
                TvPath = "tv",
                MoviePath = "mov",
                Format = "{animeRomaji}\\{animeRomaji} - {episodeNumber}{'v'fileVersion} - {episodeEnglish...} [{groupShort}][{resolution}][{videoCodec}][{crc32}]"
            }
        };
        pathBuilderRepository = new PathBuilderRepository(config);
    }

    [TestMethod]
    public void DefaultBuilderPointsToDestination()
    {
        Assert.AreEqual(@"F:\s\a", pathBuilderRepository.DefaultPathBuilder.Root);
    }

    [TestMethod]
    public void CreatedBuildersForEachPath()
    {
        var fBuilder = pathBuilderRepository.GetPathBuilderForPath(@"F:\s\a\fireforce1.mkv");
        var gBuilder = pathBuilderRepository.GetPathBuilderForPath(@"G:\s\a\fireforce1.mkv");
        var hBuilder = pathBuilderRepository.GetPathBuilderForPath(@"H:\s\a\fireforce1.mkv");
        
        Assert.AreEqual(@"F:\s\a", fBuilder.Root);
        Assert.AreEqual(@"G:\s\a", gBuilder.Root);
        Assert.AreEqual(@"H:\s\a", hBuilder.Root);
    }

    [TestMethod]
    public void GetsDefaultForUnknownPath()
    {
        var pathBuilder = pathBuilderRepository.GetPathBuilderForPath(@"C:\s\a\fireforce1.mkv");
        Assert.AreEqual(pathBuilderRepository.DefaultPathBuilder, pathBuilder);
    }
}
