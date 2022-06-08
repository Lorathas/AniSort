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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using AniDbSharp;
using AniDbSharp.Data;
using AniSort.Core;
using AniSort.Core.Commands;
using AniSort.Core.Crypto;
using AniSort.Core.Data;
using AniSort.Core.Data.Repositories;
using AniSort.Core.Exceptions;
using AniSort.Core.Extensions;
using AniSort.Core.Helpers;
using AniSort.Core.IO;
using AniSort.Core.MaintenanceTasks;
using AniSort.Core.Models;
using AniSort.Core.Utils;
using FFMpegCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AniSort;

internal class Program
{
    private const string UsageText =
        @"Usage: anisort.exe [command] [-d | --debug] [-v | --verbose] [-h | --hash] <-u username> <-p password> <paths...>
paths           paths to process files for
-c  --config    config file path
-u  --username  anidb username
-p  --password  anidb password
-h  --hash      hash files and output hashes to console
-d  --debug     enable debug mode to leave files intact and to output suggested actions
-v  --verbose   enable verbose logging";

    private static ILogger<Program> logger;

    private static FileImportUtils fileImportUtils;

    private static IServiceProvider serviceProvider;

    private static List<FileImportStatus> importedFiles;

    private static ConsoleProgressBar hashProgressBar;

    private static Dictionary<string, Type> commands;

    /*private static void Main(string[] args)
    {
        var configFileLoaded = false;
        string command = null;

        var config = new Config();

        for (var idx = 0; idx < args.Length; idx++)
        {
            var arg = args[idx];

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
                var configFilePath = args[idx + 1];

                var serializer = new XmlSerializer(typeof(Config));

                try
                {
                    if (!File.Exists(configFilePath))
                    {
                        if (EnvironmentHelpers.IsConsolePresent)
                        {
                            Console.WriteLine($"No config file found at {configFilePath}");
                        }
                        Environment.Exit(ExitCodes.InvalidXmlConfig);
                    }

                    using var fs = File.OpenRead(configFilePath);
                    Config tempConfig;
                    try
                    {
                        tempConfig = (Config)serializer.Deserialize(fs);
                        if (tempConfig == null)
                        {
                            if (EnvironmentHelpers.IsConsolePresent)
                            {
                                Console.WriteLine($"Invalid config file found at {configFilePath}");
                            }
                            Environment.Exit(ExitCodes.InvalidXmlConfig);
                            return;
                        }
                    }
                    catch (Exception ex)
                    {
                        if (EnvironmentHelpers.IsConsolePresent)
                        {
                            Console.WriteLine($"Invalid config file found at {configFilePath}: {ex.Message}");
                        }
                        Environment.Exit(ExitCodes.InvalidXmlConfig);
                        return;
                    }

                    configFileLoaded = true;

                    if (config.Debug)
                    {
                        tempConfig.Debug = true;
                    }

                    if (config.Verbose)
                    {
                        tempConfig.Verbose = true;
                    }

                    if (config.Mode != Mode.Normal)
                    {
                        tempConfig.Mode = config.Mode;
                    }

                    if (!string.IsNullOrWhiteSpace(config.AniDb.Username) && !string.IsNullOrWhiteSpace(config.AniDb.Password))
                    {
                        tempConfig.AniDb.Username = config.AniDb.Username;
                        tempConfig.AniDb.Password = config.AniDb.Password;
                    }

                    config = tempConfig;
                }
                catch (XmlException ex)
                {
                    logger.LogCritical(ex, "An error occured when parsing XML of config file at {ConfigFilePath}: {XmlException}", configFilePath, ex.Message);
                    Environment.Exit(ExitCodes.InvalidXmlConfig);
                }

                break;
            }
            else if (idx == 0 && !arg.StartsWith('-'))
            {
                command = arg;
            }
            else
            {
                config.Sources.Add(arg);
            }
        }

        if (!configFileLoaded)
        {
            foreach (var configFilePath in AppPaths.DefaultConfigFilePaths)
            {
                if (!File.Exists(configFilePath))
                {
                    continue;
                }

                if (Path.GetExtension(configFilePath) == ".xml")
                {
                    try
                    {
                        var serializer = new XmlSerializer(typeof(Config));
                        var tempConfig = (Config)serializer.Deserialize(File.OpenRead(configFilePath));

                        if (config.Debug)
                        {
                            tempConfig.Debug = true;
                        }

                        if (config.Verbose)
                        {
                            tempConfig.Verbose = true;
                        }

                        if (config.Mode != Mode.Normal)
                        {
                            tempConfig.Mode = config.Mode;
                        }

                        if (!string.IsNullOrWhiteSpace(config.AniDb.Username) && !string.IsNullOrWhiteSpace(config.AniDb.Password))
                        {
                            tempConfig.AniDb.Username = config.AniDb.Username;
                            tempConfig.AniDb.Password = config.AniDb.Password;
                        }

                        config = tempConfig;
                        configFileLoaded = true;
                        break;
                    }
                    catch (Exception)
                    {
                        // ignored
                    }
                }
            }

            if (!configFileLoaded && config.IsValid)
            {
                PrintUsageAndExit();
                Environment.Exit(ExitCodes.NoConfigFileProvided);
            }
        }

        if (!config?.IsValid ?? false)
        {
            PrintUsageAndExit();
        }

        AnimeFileStore animeFileStore = null;

        if (File.Exists(AppPaths.AnimeInfoFilePath))
        {
            animeFileStore = new AnimeFileStore();
            animeFileStore.Initialize();
        }

        Startup.Initialize(config);

        importedFiles = fileImportUtils.LoadImportedFiles();

        if (!string.IsNullOrWhiteSpace(command))
        {
            try
            {
                if (animeFileStore != null || importedFiles != null)
                {
                    var migrateTask = AddExistingDataToDatabaseAsync(animeFileStore, importedFiles);
                    migrateTask.Wait();
                }

                if (!commands.TryGetValue(command, out var commandType))
                {
                    if (EnvironmentHelpers.IsConsolePresent)
                    {
                        Console.WriteLine($"No command found for {command}");
                    }
                    Environment.Exit(ExitCodes.CommandNotFound);
                }

                var instance = serviceProvider.GetService(commandType) as ICommand;

                var task = instance.RunAsync();
                task.Wait();
            }
            catch (AggregateException ex)
            {
                using var logScope = logger.BeginScope("Aggregate exception occurred while executing sorter functionality");
                ex.Handle(iex =>
                {
                    logger.LogError(ex, "An unhandled error occurred while executing sorter functionality");
                    return false;
                });
            }
        }
        else
        {
            try
            {
                if (animeFileStore != null || importedFiles != null)
                {
                    var migrateTask = AddExistingDataToDatabaseAsync(animeFileStore, importedFiles);
                    migrateTask.Wait();
                }

                var task = RunAsync(config);
                task.Wait();
            }
            catch (AggregateException ex)
            {
                using var logScope = logger.BeginScope("Aggregate exception occurred while executing sorter functionality");
                ex.Handle(iex =>
                {
                    logger.LogError(ex, "An unhandled error occurred while executing sorter functionality");
                    return false;
                });
            }
        }
    }*/

    public static void Main(string[] args) => new Cli(Startup.Initialize(null)).Main(args);
}
