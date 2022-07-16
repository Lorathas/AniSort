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
using AniSort.Core.Data;
using AniSort.Core.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace AniSort.CoreTests.Defragmentation;

[TestClass()]
public class LowestEpisodeDefragmentationStrategyTests
{
    private static List<string> FirstPaths = new()
    {
        @"F:\s\a\tv\Kaguya-sama wa Kokurasetai_ Ultra Romantic\Kaguya-sama wa Kokurasetai_ Ultra Romantic - 01 - Miko Iino Wants to Be Soothed _ Kaguya Doesn't Realize _ Chika Fujiwara Wants to Battle [SubsPlease][1920x1080][H264_AVC][9816946B].mkv",
        @"F:\s\a\tv\Kaguya-sama wa Kokurasetai_ Ultra Romantic\Kaguya-sama wa Kokurasetai_ Ultra Romantic - 02 - Miyuki Shirogane Wants to Mediate _ Kaguya Wants to Distract Him _ Kaguya Preemptively Strikes [SubsPlease][1920x1080][H264_AVC][55185F3B].mkv",
        @"F:\s\a\tv\Kaguya-sama wa Kokurasetai_ Ultra Romantic\Kaguya-sama wa Kokurasetai_ Ultra Romantic - 04 - Kaguya Shinomiya's Impossible Demand_ _A Cowrie a Swallow Gave Birth To_ Part 1 _ Yu Ishigami Wants t... [SubsPlease][1920x1080][H264_AVC][D84F1A5E].mkv",
        @"F:\s\a\tv\Kaguya-sama wa Kokurasetai_ Ultra Romantic\Kaguya-sama wa Kokurasetai_ Ultra Romantic - 05 - Chika Fujiwara Wants to Beat a Rhythm _ Ai Hayasaka Wants to Talk _ Maki Shijo Wants Some Help [SubsPlease][1920x1080][H264_AVC][2A95851B].mkv",
        @"F:\s\a\tv\Kaguya-sama wa Kokurasetai_ Ultra Romantic\Kaguya-sama wa Kokurasetai_ Ultra Romantic - 06 - The Student Council Wants to Move Forward _ Miyuki Shirogane Wants to Make Her Confess, Part 2 _ Miyu... [SubsPlease][1920x1080][H264_AVC][F009C116].mkv",
    };

    private static List<string> SecondPaths = new()
    {
        @"G:\s\a\tv\Kaguya-sama wa Kokurasetai_ Ultra Romantic\Kaguya-sama wa Kokurasetai_ Ultra Romantic - 07 - Miko Iino Can't Love, Part 1 _ Students Wish to Discuss the Culture Festival _ Miyuki Shirogane Wants... [SubsPlease][1920x1080][H264_AVC][541930C1].mkv",
        @"G:\s\a\tv\Kaguya-sama wa Kokurasetai_ Ultra Romantic\Kaguya-sama wa Kokurasetai_ Ultra Romantic - 08 - Kei Shirogane Wants to Show Off _ About Kaguya Shinomiya, Part 2 _ Kaguya Wants to Confess [SubsPlease][1920x1080][H264_AVC][102AAF2B].mkv",
        @"G:\s\a\tv\Kaguya-sama wa Kokurasetai_ Ultra Romantic\Kaguya-sama wa Kokurasetai_ Ultra Romantic - 09 - Spring of First Year _ Kaguya's Culture Festival _ Yu Ishigami's Culture Festival [SubsPlease][1920x1080][H264_AVC][C003C634].mkv",
        @"G:\s\a\tv\Kaguya-sama wa Kokurasetai_ Ultra Romantic\Kaguya-sama wa Kokurasetai_ Ultra Romantic - 10 - Kozue Makihara Wants to Have Fun _ Chika Fujiwara Wants to Unmask _ Miyuki Shirogane's Culture Festival [SubsPlease][1920x1080][H264_AVC][741C0FC8].mkv"
    };

    private Mock<IPathBuilderRepository> pathBuilderRepositoryMock;
    private Mock<IPathBuilder> firstPathBuilderMock;
    private Mock<IPathBuilder> secondPathBuilderMock;
    private List<LocalFile> files;

    [ClassInitialize()]
    public void Setup()
    {
        firstPathBuilderMock = new Mock<IPathBuilder>();
        firstPathBuilderMock
            .Setup(b => b.Root)
            .Returns(@"F:\s\a");
        secondPathBuilderMock = new Mock<IPathBuilder>();
        secondPathBuilderMock
            .Setup(b => b.Root)
            .Returns(@"G:\s\a");

        pathBuilderRepositoryMock = new Mock<IPathBuilderRepository>();
        pathBuilderRepositoryMock
            .Setup(b => b.GetPathBuilderForPath(It.Is<string>(p => FirstPaths.Contains(p))))
            .Returns(firstPathBuilderMock.Object);
        pathBuilderRepositoryMock
            .Setup(b => b.GetPathBuilderForPath(It.Is<string>(p => !FirstPaths.Contains(p))))
            .Returns(secondPathBuilderMock.Object);

        files = new List<LocalFile>();
        var transform = (string path) => new LocalFile
        {
            Path = path,
            EpisodeFile = new EpisodeFile
            {
                Episode = new Episode
                {
                    Number = path.Substring(98, 2)
                }
            }
        };
    }

    [TestMethod()]
    public void TestSingleEpisode()
    {

    }
}
