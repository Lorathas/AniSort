﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Xml.Serialization;
using AniSort.Core.Helpers;
using AniSort.Core.MaintenanceTasks;
using Microsoft.Extensions.CommandLineUtils;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AniSort.Core.Commands;

public class MaintenanceCommand : ICommand
{
    private readonly IServiceProvider serviceProvider;
    private readonly ILogger<MaintenanceCommand> logger;

    public MaintenanceCommand(IServiceProvider serviceProvider, ILogger<MaintenanceCommand> logger)
    {
        this.serviceProvider = serviceProvider;
        this.logger = logger;
    }

    /// <inheritdoc />
    public async Task RunAsync(List<CommandOption> commandOptions)
    {
        foreach (var maintenanceTaskType in AssemblyHelpers.MaintenanceTaskTypes)
        {
            await using var scope = serviceProvider.CreateAsyncScope();

            var instance = scope.ServiceProvider.GetService(maintenanceTaskType);

            if (instance is not IMaintenanceTask maintenanceTask)
            {
                logger.LogWarning("Maintenance Task of type {MaintenanceTaskType} is not injectable", maintenanceTaskType.FullName);
                continue;
            }

            logger.LogDebug("Running maintenance task: {MaintenanceTask}", maintenanceTask.Description);
            await maintenanceTask.RunAsync();
            logger.LogInformation("Ran maintenance task: {MaintenanceTask}", maintenanceTask.Description);
        }
    }

    /// <inheritdoc />
    public IEnumerable<string> CommandNames => new[] { "maint", "maintenance" };

    /// <inheritdoc />
    public string HelpOption => "-h --help";

    /// <inheritdoc />
    public bool IncludeCredentialOptions => false;

    /// <inheritdoc />
    public List<CommandOption> SetupCommand(CommandLineApplication command)
    {
        foreach (var maintenanceTaskType in AssemblyHelpers.MaintenanceTaskTypes)
        {
            using var scope = serviceProvider.CreateScope();
            var instance = scope.ServiceProvider.GetService(maintenanceTaskType);

            if (instance is not IMaintenanceTask maintenanceTask)
            {
                logger.LogWarning("Maintenance Task of type {MaintenanceTaskType} is not injectable", maintenanceTaskType.FullName);
                continue;
            }

            command.Command(maintenanceTask.CommandName, (subCommand) =>
            {
                subCommand.Options.Add(Cli.ConfigOption);
                subCommand.Options.Add(Cli.DebugOption);
                subCommand.Options.Add(Cli.VerboseOption);

                subCommand.OnExecute(async () =>
                {
                    Config config = default;
                    var serializer = new XmlSerializer(typeof(Config));

                    if (Cli.ConfigOption.HasValue())
                    {
                        await using var fs = File.OpenRead(Cli.ConfigOption.Value());

                        config = serializer.Deserialize(fs) as Config;

                        if (config == default)
                        {
                            logger.LogCritical("No valid config found at provided path: {ConfigFilePAth}", Cli.ConfigOption.Value());
                            Environment.Exit(ExitCodes.NoConfigFileProvided);
                        }
                    }
                    else
                    {
                        foreach (string path in AppPaths.DefaultConfigFilePaths)
                        {
                            if (!File.Exists(path))
                            {
                                continue;
                            }

                            await using var fs = File.OpenRead(path);
                            config = serializer.Deserialize(fs) as Config;
                            break;
                        }

                        if (config == default)
                        {
                            logger.LogCritical("No config found in default paths: {DefaultConfigFilePaths}", string.Join(',', AppPaths.DefaultConfigFilePaths));
                            Environment.Exit(ExitCodes.NoConfigFileProvided);
                        }
                    }

                    if (Cli.DebugOption.HasValue())
                    {
                        config.Debug = true;
                    }

                    if (Cli.VerboseOption.HasValue())
                    {
                        config.Verbose = true;
                    }

                    var configServiceProvider = Startup.InitializeServices(config);
                    var fullMaintenanceTask = configServiceProvider.GetService(maintenanceTaskType) as IMaintenanceTask;

                    if (fullMaintenanceTask == default)
                    {
                        logger.LogCritical("Maintenance Task of type {MaintenanceTaskType} is not injectable", maintenanceTaskType.FullName);
                        Environment.Exit(ExitCodes.CommandNotInstantiable);
                    }

                    logger.LogDebug("Running maintenance task: {MaintenanceTask}", fullMaintenanceTask.Description);
                    await fullMaintenanceTask.RunAsync();
                    logger.LogInformation("Ran maintenance task: {MaintenanceTask}", fullMaintenanceTask.Description);
                    return 0;
                });
            });
        }

        return new List<CommandOption>();
    }
}
