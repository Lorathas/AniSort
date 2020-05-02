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
using System.Diagnostics;
using System.IO;
using System.IO.Enumeration;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using AniDbSharp;
using AniDbSharp.Data;
using AniSort.Core.Crypto;
using AniSort.Core.Extensions;
using AniSort.Extensions;

namespace AniSort
{
    class Program
    {
        private const string UsageText =
            @"Usage: anisort.exe [-d | --debug] [-v | --verbose] [-h | --hash] <-u username> <-p password> <paths...>
paths           paths to process files for
-u  --username  anidb username
-p  --password  anidb password
-h  --hash      hash files and output hashes to console
-d  --debug     enable debug mode to leave files intact and to output suggested actions
-v  --verbose   enable verbose logging";

        private static readonly string[] SupportedFileExtensions =
        {
            ".mkv", ".mp4", ".avi", ".wmv", ".mov", ".mpeg", ".mpg", ".flv", ".webm", ".ogg", ".vob", ".m2ts", ".mts",
            ".ts", ".yuv", ".rm", ".rmvb", ".m4v", ".m4p", ".mp2", ".mpe", ".mpv", ".m2v", ".svi", ".3gp", ".3g2",
            ".mxf", ".nsv", ".f4v", ".f4p", ".f4a", ".f4b"
        };

        static void Main(string[] args)
        {
            var options = new Options();

            for (int idx = 0; idx < args.Length; idx++)
            {
                string arg = args[idx];

                if (string.Equals(arg, "-d") || string.Equals(arg, "--debug"))
                {
                    options.DebugMode = true;
                }
                else if (string.Equals(arg, "-v") || string.Equals(arg, "--verbose"))
                {
                    options.VerboseLogging = true;
                }
                else if (string.Equals(arg, "-h") || string.Equals(arg, "--hash"))
                {
                    options.Mode = Mode.Hash;
                }
                else if (string.Equals(arg, "-u") || string.Equals(arg, "--username"))
                {
                    if (idx == args.Length - 1)
                    {
                        PrintUsageAndExit();
                    }

                    options.Username = args[idx + 1];
                    idx++;
                }
                else if (string.Equals(arg, "-p") || string.Equals(arg, "--password"))
                {
                    if (idx == args.Length - 1)
                    {
                        PrintUsageAndExit();
                    }

                    options.Password = args[idx + 1];
                    idx++;
                }
                else
                {
                    options.Sources.Add(arg);
                }
            }

            if (!options.IsValid)
            {
                PrintUsageAndExit();
            }

            try
            {
                var task = RunAsync(options);
                task.Wait();
            }
            catch (AggregateException ex)
            {
                ex.Handle((iex) =>
                {
                    Console.WriteLine(iex.Message);
                    return false;
                });
            }
        }

        private static void PrintUsageAndExit()
        {
            Console.Write(UsageText);
            Environment.Exit(0);
        }

        private static async Task RunAsync(Options options)
        {
            switch (options.Mode)
            {
                case Mode.Normal:
                    await RunNormalAsync(options);
                    break;
                case Mode.Hash:
                    RunHashes(options);
                    break;
            }
        }

        private static async Task RunNormalAsync(Options options)
        {
            var fileQueue = new Queue<string>();

            AddPathsToQueue(options.Sources, fileQueue);

            var client = new AniDbClient("anidbapiclient", 1, options.Username, options.Password);

            try
            {
                client.Connect();
                var auth = await client.AuthAsync();

                if (!auth.Success)
                {
                    throw new Exception("RIP");
                }

                while (fileQueue.TryDequeue(out string path))
                {
                    try
                    {
                        string filename = Path.GetFileName(path);

                        using (var fs = new BufferedStream(File.OpenRead(path)))
                        {
                            long totalBytes = fs.Length;

                            hashProgressBar = new ConsoleProgressBar(totalBytes, 40, postfixMessage: $"hashing: {path}",
                                postfixMessageShort: $"hashing: {filename}");

                            var sw = Stopwatch.StartNew();

                            var hashTask = Ed2k.HashMultiAsync(fs, new Progress<long>(OnProgressUpdate));

                            while (!hashTask.IsCompleted)
                            {
                                hashProgressBar.WriteNextFrame();

                                Thread.Sleep(TimeSpan.FromMilliseconds(100));
                            }

                            hashProgressBar = null;

                            hashTask.Wait();

                            byte[] hash = hashTask.Result;

                            sw.Stop();

                            Console.WriteLine(
                                $"\rHashed: {(path.Length + 8 > Console.WindowWidth ? filename : path).Truncate(Console.WindowWidth)}");
                            Console.WriteLine($"  eD2k hash: {hash.ToHexString()}");

                            if (options.VerboseLogging)
                            {
                                Console.WriteLine(
                                    $"  Processed {(double)totalBytes / 1024 / 1024:###,###,##0.00}MB in {sw.Elapsed} at a rate of {Math.Round(((double)totalBytes / 1024 / 1024) / sw.Elapsed.TotalSeconds):F2}MB/s");
                            }

                            var result =
                                await client.SearchForFile(totalBytes, hash,
                                    new FileMask(FileMaskFirstByte.IsDeprecated | FileMaskFirstByte.State,
                                        FileMaskSecondByte.Crc32,
                                        FileMaskThirdByte.Quality | FileMaskThirdByte.Source |
                                        FileMaskThirdByte.VideoCodec, 0, 0),
                                    new FileAnimeMask(
                                        FileAnimeMaskFirstByte.HighestEpisodeNumber |
                                        FileAnimeMaskFirstByte.TotalEpisodes | FileAnimeMaskFirstByte.Type,
                                        FileAnimeMaskSecondByte.RomajiName,
                                        FileAnimeMaskThirdByte.EpisodeName | FileAnimeMaskThirdByte.EpisodeNumber,
                                        FileAnimeMaskFourthByte.GroupShortName));

                            if (!result.FileFound)
                            {
                                Console.WriteLine($"No file found for {filename}".Truncate(Console.WindowWidth));
                                Console.WriteLine();
                                continue;
                            }

                            Console.WriteLine($"File found for {filename}");

                            if (options.VerboseLogging)
                            {
                                Console.WriteLine($"  Anime: {result.AnimeInfo.RomajiName}");
                                Console.WriteLine(
                                    $"  Episode: {result.AnimeInfo.EpisodeNumber:##} {result.AnimeInfo.EpisodeName}");
                                Console.WriteLine($"  CRC32: {result.FileInfo.Crc32Hash.ToHexString()}");
                                Console.WriteLine($"  Group: {result.AnimeInfo.GroupShortName}");
                            }

                            Console.WriteLine();
                        }
                    }
                    catch (Exception ex)
                    {
                    }
                }
            }
            finally
            {
                client.Dispose();
            }

            Console.WriteLine("Finished processing all files. Exiting...");
        }

        private static void AddPathsToQueue(IEnumerable<string> paths, Queue<string> queue)
        {
            foreach (string path in paths)
            {
                if (Directory.Exists(path))
                {
                    AddPathsToQueue(Directory.GetFiles(path), queue);
                }
                else if (SupportedFileExtensions.Contains(Path.GetExtension(path)) && File.Exists(path))
                {
                    queue.Enqueue(path);
                }
            }
        }

        private static ConsoleProgressBar hashProgressBar = null;

        private static void OnProgressUpdate(long bytesProcessed)
        {
            if (hashProgressBar != null)
            {
                hashProgressBar.Progress = bytesProcessed;
            }
        }

        private static void RunHashes(Options options)
        {
            var fileQueue = new Queue<string>();

            AddPathsToQueue(options.Sources, fileQueue);

            while (fileQueue.TryDequeue(out string path))
            {
                using (var fs = new BufferedStream(File.OpenRead(path)))
                {
                    long totalBytes = fs.Length;

                    hashProgressBar = new ConsoleProgressBar(totalBytes, 40, postfixMessage: $"hashing: {path}",
                        postfixMessageShort: $"hashing: {Path.GetFileName(path)}");

                    var sw = Stopwatch.StartNew();

                    var hashTask = Ed2k.HashMultiAsync(fs, new Progress<long>(OnProgressUpdate));

                    while (!hashTask.IsCompleted)
                    {
                        hashProgressBar.WriteNextFrame();

                        Thread.Sleep(TimeSpan.FromMilliseconds(100));
                    }

                    hashTask.Wait();

                    byte[] hash = hashTask.Result;

                    hashProgressBar = null;

                    sw.Stop();

                    Console.WriteLine(
                        $"\rHashed: {(path.Length + 8 > Console.WindowWidth ? Path.GetFileName(path) : path).Truncate(Console.WindowWidth)}");
                    Console.WriteLine($"  eD2k hash: {hash.ToHexString()}");

                    if (options.VerboseLogging)
                    {
                        Console.WriteLine(
                            $"  Processed {(double) totalBytes / 1024 / 1024:###,###,##0.00}MB in {sw.Elapsed} at a rate of {Math.Round(((double) totalBytes / 1024 / 1024) / sw.Elapsed.TotalSeconds):F2}MB/s");
                    }

                    Console.WriteLine();
                }
            }

            Console.WriteLine("Finished hashing all files. Exiting...");
        }
    }
}