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
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Enumeration;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using AniDbSharp;
using AniDbSharp.Data;
using AniSort.Core;
using AniSort.Core.Crypto;
using AniSort.Core.Exceptions;
using AniSort.Core.Extensions;
using AniSort.Core.IO;
using AniSort.Core.Models;
using AniSort.Core.Utils;
using AniSort.Extensions;
using CsvHelper;

namespace AniSort
{
    class Program
    {
        private const string UsageText =
            @"Usage: anisort.exe [-d | --debug] [-v | --verbose] [-h | --hash] <-u username> <-p password> <paths...>
paths           paths to process files for
-c  --config    config file path
-u  --username  anidb username
-p  --password  anidb password
-h  --hash      hash files and output hashes to console
-d  --debug     enable debug mode to leave files intact and to output suggested actions
-v  --verbose   enable verbose logging";

        private const string ApiClientName = "anidbapiclient";

        private const int ApiClientVersion = 1;

        private static readonly string[] SupportedFileExtensions =
        {
            ".mkv", ".mp4", ".avi", ".wmv", ".mov", ".mpeg", ".mpg", ".flv", ".webm", ".ogg", ".vob", ".m2ts", ".mts",
            ".ts", ".yuv", ".rm", ".rmvb", ".m4v", ".m4p", ".mp2", ".mpe", ".mpv", ".m2v", ".svi", ".3gp", ".3g2",
            ".mxf", ".nsv", ".f4v", ".f4p", ".f4a", ".f4b"
        };

        private static List<FileImportStatus> importedFiles;

        static void Main(string[] args)
        {
            AppPaths.Initialize();

            importedFiles = FileImportUtils.LoadImportedFiles();

            var config = new Config();

            for (int idx = 0; idx < args.Length; idx++)
            {
                string arg = args[idx];

                if (string.Equals(arg, "-d") || string.Equals(arg, "--debug"))
                {
                    config.Debug = true;
                }
                else if (string.Equals(arg, "-v") || string.Equals(arg, "--verbose"))
                {
                    config.Verbose = true;
                }
                else if (string.Equals(arg, "-h") || string.Equals(arg, "--hash"))
                {
                    config.Mode = Mode.Hash;
                }
                else if (string.Equals(arg, "-u") || string.Equals(arg, "--username"))
                {
                    if (idx == args.Length - 1)
                    {
                        PrintUsageAndExit();
                    }

                    config.AniDb.Username = args[idx + 1];
                    idx++;
                }
                else if (string.Equals(arg, "-p") || string.Equals(arg, "--password"))
                {
                    if (idx == args.Length - 1)
                    {
                        PrintUsageAndExit();
                    }

                    config.AniDb.Password = args[idx + 1];
                    idx++;
                }
                else if (string.Equals(arg, "-c") || string.Equals(arg, "--config"))
                {
                    string configFilePath = args[idx + 1];

                    var serializer = new XmlSerializer(typeof(Config));

                    try
                    {
                        if (!File.Exists(configFilePath))
                        {
                            Console.WriteLine($"File does not exist for path: {configFilePath}");
                        }

                        using var fs = File.OpenRead(configFilePath);
                        config = (Config) serializer.Deserialize(fs);
                    }
                    catch (XmlException ex)
                    {
                        Console.WriteLine($"Invalid XML config file: {ex.Message}");
                        Environment.Exit(0);
                    }

                    break;
                }
                else
                {
                    config.Sources.Add(arg);
                }
            }

            if (!config.IsValid)
            {
                PrintUsageAndExit();
            }

            try
            {
                var task = RunAsync(config);
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

        private static async Task RunAsync(Config config)
        {
            switch (config.Mode)
            {
                case Mode.Normal:
                    await RunNormalAsync(config);
                    break;
                case Mode.Hash:
                    RunHashes(config);
                    break;
            }
        }

        private static async Task RunNormalAsync(Config config)
        {
            var fileQueue = new Queue<string>();

            AddPathsToQueue(config.Sources, fileQueue);

            PathBuilder pathBuilder = null;

            try
            {
                pathBuilder = PathBuilder.Compile(config.Destination.Path, config.Destination.TvPath,
                    config.Destination.MoviePath, config.Destination.Format);
            }
            catch (InvalidFormatPathException ex)
            {
                Console.WriteLine(ex.Message);
                Environment.Exit(0);
            }

            if (config.Verbose)
            {
                Console.WriteLine("Config setup to write to following directories for files:");
                Console.WriteLine($"  TV:     {Path.Combine(config.Destination.Path, config.Destination.TvPath)}");
                Console.WriteLine($"  Movies: {Path.Combine(config.Destination.Path, config.Destination.MoviePath)}");
                Console.WriteLine($"Path builder base path: {pathBuilder.Root}");
            }

            var client = new AniDbClient(ApiClientName, ApiClientVersion, config.AniDb.Username, config.AniDb.Password);

            try
            {
                client.Connect();
                var auth = await client.AuthAsync();

                if (!auth.Success)
                {
                    Console.WriteLine("Invalid auth credentials");
                    Environment.Exit(0);
                }

                if (auth.HasNewVersion)
                {
                    Console.WriteLine("A new version of the software is available. Please download it when possible");
                }

                while (fileQueue.TryDequeue(out string path))
                {
                    try
                    {
                        string filename = Path.GetFileName(path);

                        var fileImportStatus = importedFiles.FirstOrDefault(i => i.FilePath == path);

                        if (fileImportStatus == null)
                        {
                            fileImportStatus = new FileImportStatus(path);
                            importedFiles.Add(fileImportStatus);
                        }

                        if (fileImportStatus.Status == ImportStatus.Imported)
                        {
                            Console.WriteLine($"File \"{path}\" has already been imported. Skipping...");
                            Console.WriteLine();
                            continue;
                        }

                        byte[] hash;
                        long totalBytes;
                        if (fileImportStatus.Hash != null)
                        {
                            hash = fileImportStatus.Hash;
                            totalBytes = fileImportStatus.FileLength;

                            Console.WriteLine($"File \"{path}\" already hashed. Skipping hashing process...");
                        }
                        else
                        {
                            await using var fs = new BufferedStream(File.OpenRead(path));
                            fileImportStatus.FileLength = totalBytes = fs.Length;

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

                            fileImportStatus.Hash = hash = hashTask.Result;

                            sw.Stop();


                            Console.WriteLine(
                                $"\rHashed: {(path.Length + 8 > Console.WindowWidth ? filename : path).Truncate(Console.WindowWidth)}");
                            Console.WriteLine($"  eD2k hash: {hash.ToHexString()}");

                            if (config.Verbose)
                            {
                                Console.WriteLine(
                                    $"  Processed {(double) totalBytes / 1024 / 1024:###,###,##0.00}MB in {sw.Elapsed} at a rate of {Math.Round(((double) totalBytes / 1024 / 1024) / sw.Elapsed.TotalSeconds):F2}MB/s");
                            }
                        }

                        var result =
                            await client.SearchForFile(totalBytes, hash, pathBuilder.FileMask, pathBuilder.AnimeMask);

                        if (!result.FileFound)
                        {
                            Console.WriteLine($"No file found for {filename}".Truncate(Console.WindowWidth));
                            Console.WriteLine();
                            fileImportStatus.Status = ImportStatus.NoFileFound;
                            FileImportUtils.UpdateImportedFiles(importedFiles);
                            continue;
                        }

                        Console.WriteLine($"File found for {filename}");

                        if (config.Verbose)
                        {
                            Console.WriteLine($"  Anime: {result.AnimeInfo.RomajiName}");
                            Console.WriteLine(
                                $"  Episode: {result.AnimeInfo.EpisodeNumber:##} {result.AnimeInfo.EpisodeName}");
                            Console.WriteLine($"  CRC32: {result.FileInfo.Crc32Hash.ToHexString()}");
                            Console.WriteLine($"  Group: {result.AnimeInfo.GroupShortName}");
                        }

                        string extension = Path.GetExtension(filename);

                        // Trailing dot is there to prevent Path.ChangeExtension from screwing with the path if it has been ellipsized or has ellipsis in it
                        string destinationPath = pathBuilder.BuildPath(result.FileInfo, result.AnimeInfo,
                            PlatformUtils.MaxPathLength - extension.Length);

                        string destinationFilename = destinationPath + extension;
                        string destinationDirectory = Path.GetDirectoryName(destinationPath);

                        if (!config.Debug && !Directory.Exists(destinationDirectory))
                        {
                            try
                            {
                                Directory.CreateDirectory(destinationDirectory);
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine(
                                    "An unknown error occurred while trying to created the directory. Please make sure the program has access to the target directory: " +
                                    ex.Message);
                                Console.WriteLine();
                                fileImportStatus.Status = ImportStatus.Error;
                                fileImportStatus.Message = ex.Message;
                                fileImportStatus.Attempts++;
                                FileImportUtils.UpdateImportedFiles(importedFiles);
                                continue;
                            }
                        }

                        if (File.Exists(destinationFilename))
                        {
                            fileImportStatus.Status = ImportStatus.Imported;
                            fileImportStatus.Message = destinationFilename;
                            fileImportStatus.Attempts++;
                            FileImportUtils.UpdateImportedFiles(importedFiles);
                            Console.WriteLine(
                                $"Destination file \"{destinationFilename}\" already exists. Skipping...");
                        }
                        else if (config.Copy)
                        {
                            if (!config.Debug)
                            {
                                try
                                {
                                    if (config.Verbose)
                                    {
                                        Console.WriteLine($"Destination Path: {destinationFilename}");
                                    }

                                    File.Copy(path, destinationFilename);
                                }
                                catch (UnauthorizedAccessException ex)
                                {
                                    Console.WriteLine(
                                        "You do not have access to the destination path. Please ensure your user account has access to the destination folder.");
                                    Console.WriteLine();
                                    fileImportStatus.Status = ImportStatus.Error;
                                    fileImportStatus.Message = ex.Message;
                                    fileImportStatus.Attempts++;
                                    FileImportUtils.UpdateImportedFiles(importedFiles);
                                    continue;
                                }
                                catch (PathTooLongException ex)
                                {
                                    Console.WriteLine(
                                        "Filename too long. Yell at Lorathas to fix path length checking if this keeps occurring.");
                                    Console.WriteLine();
                                    fileImportStatus.Status = ImportStatus.Error;
                                    fileImportStatus.Message = ex.Message;
                                    fileImportStatus.Attempts++;
                                    FileImportUtils.UpdateImportedFiles(importedFiles);
                                    continue;
                                }
                                catch (IOException ex)
                                {
                                    Console.WriteLine($"An unhandled I/O error has occurred: {ex.Message}");
                                    Console.WriteLine();
                                    fileImportStatus.Status = ImportStatus.Error;
                                    fileImportStatus.Message = ex.Message;
                                    fileImportStatus.Attempts++;
                                    FileImportUtils.UpdateImportedFiles(importedFiles);
                                    continue;
                                }
                            }

                            fileImportStatus.Status = ImportStatus.Imported;
                            fileImportStatus.Message = destinationFilename;
                            fileImportStatus.Attempts++;
                            FileImportUtils.UpdateImportedFiles(importedFiles);
                            Console.WriteLine($"Copied {filename} to {destinationFilename}");
                        }
                        else
                        {
                            if (!config.Debug)
                            {
                                try
                                {
                                    File.Move(path, destinationFilename);
                                }
                                catch (UnauthorizedAccessException ex)
                                {
                                    Console.WriteLine(
                                        "You do not have access to the destination path. Please ensure your user account has access to the destination folder.");
                                    Console.WriteLine();
                                    fileImportStatus.Status = ImportStatus.Error;
                                    fileImportStatus.Message = ex.Message;
                                    fileImportStatus.Attempts++;
                                    FileImportUtils.UpdateImportedFiles(importedFiles);
                                    continue;
                                }
                                catch (PathTooLongException ex)
                                {
                                    Console.WriteLine(
                                        "Filename too long. Yell at Lorathas to implement path length checking if this keeps occurring.");
                                    Console.WriteLine();
                                    fileImportStatus.Status = ImportStatus.Error;
                                    fileImportStatus.Message = ex.Message;
                                    fileImportStatus.Attempts++;
                                    FileImportUtils.UpdateImportedFiles(importedFiles);
                                    continue;
                                }
                                catch (IOException ex)
                                {
                                    Console.WriteLine($"An unhandled I/O error has occurred: {ex.Message}");
                                    Console.WriteLine();
                                    fileImportStatus.Status = ImportStatus.Error;
                                    fileImportStatus.Message = ex.Message;
                                    fileImportStatus.Attempts++;
                                    FileImportUtils.UpdateImportedFiles(importedFiles);
                                    continue;
                                }
                            }

                            fileImportStatus.Status = ImportStatus.Imported;
                            fileImportStatus.Message = destinationFilename;
                            fileImportStatus.Attempts++;
                            FileImportUtils.UpdateImportedFiles(importedFiles);
                            Console.WriteLine($"Moved {filename} to {destinationFilename}");
                        }

                        Console.WriteLine();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"An unknown error has occurred: {ex.Message}");
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

        private static void RunHashes(Config config)
        {
            var fileQueue = new Queue<string>();

            AddPathsToQueue(config.Sources, fileQueue);

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

                    if (config.Verbose)
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