using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using AniDbSharp;
using AniSort.Core.Commands;
using AniSort.Core.Data;
using AniSort.Core.Data.Repositories;
using AniSort.Core.DataFlow;
using AniSort.Core.Helpers;
using AniSort.Core.IO;
using AniSort.Core.MaintenanceTasks;
using AniSort.Core.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NLog;
using NLog.Config;
using NLog.Extensions.Logging;
using NLog.Layouts;
using NLog.Targets;
using LogLevel = NLog.LogLevel;

namespace AniSort.Core;

public class Startup
{
    public static ServiceProvider ServiceProvider { get; private set; }

    private static IImmutableDictionary<string, Type> commands;

    public static void InitializeLogging(Config? aniSortConfig)
    {
        var loggingConfig = new LoggingConfiguration();

        var fileLog = new FileTarget("fileLog")
        {
            FileName = Path.Combine(AppPaths.DataPath, "anisort.log")
        };
        var errorFileLog = new FileTarget("errorFileLog")
        {
            FileName = Path.Combine(AppPaths.DataPath, "anisort.err.log"),
            Layout = new SimpleLayout("${longdate}|${level:uppercase=true}|${logger}|${message}|${exception:format=StackTrace}")
        };

        var fileAndConsoleMinLevel = LogLevel.Info;

        if (aniSortConfig?.Verbose ?? false)
        {
            fileAndConsoleMinLevel = LogLevel.Debug;
        }
        else if (aniSortConfig?.Debug ?? false)
        {
            fileAndConsoleMinLevel = LogLevel.Debug;
        }

        var nullTarget = new NullTarget();
        loggingConfig.LoggingRules.Add(new LoggingRule("Microsoft.*", LogLevel.Trace, LogLevel.Fatal, nullTarget)
        {
            Final = true
        });
        loggingConfig.LoggingRules.Add(new LoggingRule("Grpc.AspNetCore.Server.ServerCallHandler", LogLevel.Info, LogLevel.Error, nullTarget)
        {
            Final = true
        });

        loggingConfig.AddRule(fileAndConsoleMinLevel, LogLevel.Warn, fileLog);
        loggingConfig.AddRule(LogLevel.Error, LogLevel.Fatal, errorFileLog);

        if (EnvironmentHelpers.IsConsolePresent)
        {
            var consoleLog = new ColoredConsoleTarget("consoleLog");
            loggingConfig.AddRule(fileAndConsoleMinLevel, LogLevel.Fatal, consoleLog);
        }

        LogManager.Configuration = loggingConfig;
    }

    private static IServiceCollection InitializeServicesInternal(ConfigurationManager configuration, IServiceCollection? builder = null)
    {
        builder ??= new ServiceCollection();
        builder
            .AddSingleton(sp => sp) // Add service provider so it's injectable for items that need a custom scope
            .AddSingleton(typeof(FileImportUtils))
            .AddScoped<IAnimeRepository, AnimeRepository>()
            .AddScoped<IAudioCodecRepository, AudioCodecRepository>()
            .AddScoped<ICategoryRepository, CategoryRepository>()
            .AddScoped<IEpisodeFileRepository, EpisodeFileRepository>()
            .AddScoped<IEpisodeRepository, EpisodeRepository>()
            .AddScoped<IFileActionRepository, FileActionRepository>()
            .AddScoped<ILocalFileRepository, LocalFileRepository>()
            .AddScoped<IReleaseGroupRepository, ReleaseGroupRepository>()
            .AddScoped<ISynonymRepository, SynonymRepository>()
            .AddScoped<IJobRepository, JobRepository>()
            .AddScoped<IScheduledJobRepository, ScheduledJobRepository>()
            .AddScoped<IJobStepRepository, JobStepRepository>()
            .AddTransient<IPathBuilderRepository, PathBuilderRepository>()
            .AddTransient<LegacyDataStoreProvider>()
            .AddTransient<BlockProvider>()
            .AddDbContext<AniSortContext>(b => b.UseNpgsql(configuration.GetConnectionString("Postgres")))
            .AddTransient(p =>
            {
                // ReSharper disable once VariableHidesOuterVariable
                var config = p.GetService<Config>();
                return config != default && !string.IsNullOrWhiteSpace(config.AniDb.Username) && !string.IsNullOrWhiteSpace(config.AniDb.Password)
                    ? new AniDbClient(Constants.ApiClientName, Constants.ApiClientVersion, config!.AniDb.Username, config.AniDb.Password)
                    : new AniDbClient(Constants.ApiClientName, Constants.ApiClientVersion);
            })
            .AddLogging(b =>
            {
                b.ClearProviders();
                b.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Trace);
                b.AddNLog();
                b.AddFilter((category, logLevel) =>
                {
                    return !category.StartsWith("Microsoft.EntityFrameworkCore");
                });
            });

        foreach (var maintenanceTaskType in AppDomain.CurrentDomain.GetAssemblies().SelectMany(s => s.GetTypes()).Where(t => typeof(IMaintenanceTask).IsAssignableFrom(t) && !t.IsInterface))
        {
            builder = builder.AddTransient(maintenanceTaskType);
        }

        foreach (var commandType in AssemblyHelpers.CommandTypes)
        {
            builder = builder.AddTransient(commandType);
        }

        return builder;
    }

    public static ServiceProvider InitializeServices(Config? config, ConfigurationManager configuration, IServiceCollection? serviceCollection = null)
    {
        InitializeLogging(config);
        return InitializeServicesInternal(configuration, serviceCollection)
            .AddSingleton(config ?? new Config())
            .BuildServiceProvider();
    }

    private static void InitializeCommands()
    {
        using var scopedProvider = ServiceProvider.CreateScope();

        // ReSharper disable once LocalVariableHidesMember
        var commands = new Dictionary<string, Type>();
        foreach (var commandType in AssemblyHelpers.CommandTypes)
        {
            var instance = scopedProvider.ServiceProvider.GetService(commandType) as ICommand;

            if (instance == null)
            {
                if (EnvironmentHelpers.IsConsolePresent)
                {
                    Console.WriteLine($"Command for type {commandType.FullName} is not instantiable");
                }
                Environment.Exit(ExitCodes.CommandNotInstantiable);
            }

            foreach (var name in instance.CommandNames)
            {
                if (commands.ContainsKey(name))
                {
                    if (EnvironmentHelpers.IsConsolePresent)
                    {
                        Console.WriteLine($"Duplicate command exists for {name}");
                    }
                    Environment.Exit(ExitCodes.DuplicateCommandsFound);
                }
                commands[name] = commandType;
            }
        }

        Startup.commands = commands.ToImmutableDictionary();
    }

    public static IServiceProvider Initialize(Config? config, ConfigurationManager configuration, IServiceCollection? serviceCollection = null)
    {
        AppPaths.Initialize();
        ServiceProvider = InitializeServices(config, configuration, serviceCollection);
        InitializeCommands();
        return ServiceProvider;
    }
}
