using System;
using System.IO;
using System.Xml.Serialization;
using AniSort.Core.Commands;
using AniSort.Core.Helpers;
using Microsoft.Extensions.CommandLineUtils;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AniSort.Core;

public class Cli
{
    private readonly IServiceProvider serviceProvider;
    private readonly ILogger<Cli> logger;
    public static CommandOption ConfigOption { get; private set; }
    public static CommandOption DebugOption { get; private set; }
    public static CommandOption VerboseOption { get; private set; }
    public static CommandOption UsernameOption { get; private set; }
    public static CommandOption PasswordOption { get; private set; }

    public Cli(IServiceProvider serviceProvider)
    {
        this.serviceProvider = serviceProvider;
    }

    public void Main(string[] args)
    {
        var app = new CommandLineApplication
        {
            Name = "anisort",
            Description = "Anime sorter and library management"
        };
        app.HelpOption("-h --help");

        ConfigOption = app.Option("-c --config <config>", "Path to config file", CommandOptionType.SingleValue);
        DebugOption = app.Option("-d --debug", "Debug mode", CommandOptionType.NoValue);
        VerboseOption = app.Option("-v --verbose", "Verbose logging mode", CommandOptionType.NoValue);
        UsernameOption = app.Option("-u --username", "AniDB username", CommandOptionType.SingleValue);
        PasswordOption = app.Option("-p --password", "AniDB password", CommandOptionType.SingleValue);

        foreach (var commandType in AssemblyHelpers.CommandTypes)
        {
            var instance = serviceProvider.GetService(commandType) as ICommand;

            if (instance == default)
            {
                logger.LogCritical("Command {TypeName} could not be instantiated", commandType.FullName);
                Environment.Exit(ExitCodes.CommandNotInstantiable);
            }

            foreach (string commandName in instance.CommandNames)
            {
                app.Command(commandName, command =>
                {
                    command.Options.Add(ConfigOption);
                    command.Options.Add(DebugOption);
                    command.Options.Add(VerboseOption);

                    if (instance.IncludeCredentialOptions)
                    {
                        command.Options.Add(UsernameOption);
                        command.Options.Add(PasswordOption);
                    }

                    var commandOptions = instance.SetupCommand(command);

                    command.OnExecute(async () =>
                    {
                        Config config = default;
                        var serializer = new XmlSerializer(typeof(Config));

                        if (ConfigOption.HasValue())
                        {
                            await using var fs = File.OpenRead(ConfigOption.Value());

                            config = serializer.Deserialize(fs) as Config;

                            if (config == default)
                            {
                                logger.LogCritical("No valid config found at provided path: {ConfigFilePAth}", ConfigOption.Value());
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

                        if (DebugOption.HasValue())
                        {
                            config.Debug = true;
                        }

                        if (VerboseOption.HasValue())
                        {
                            config.Verbose = true;
                        }

                        if (instance.IncludeCredentialOptions)
                        {
                            if (config.AniDb == default)
                            {
                                config.AniDb = new AniDbConfig();
                            }

                            if ((!UsernameOption.HasValue() || !PasswordOption.HasValue()) && (string.IsNullOrWhiteSpace(config.AniDb.Username) || string.IsNullOrWhiteSpace(config.AniDb.Password)))
                            {
                                logger.LogCritical("Required AniDB auth credentials not provided to command line and not found in config file");
                                Environment.Exit(ExitCodes.InvalidAuthCredentials);
                            }

                            if (UsernameOption.HasValue() && PasswordOption.HasValue())
                            {
                                config.AniDb.Username = UsernameOption.Value();
                                config.AniDb.Password = PasswordOption.Value();
                            }
                        }

                        var configServiceProvider = Startup.InitializeServices(config);

                        using var scope = configServiceProvider.CreateScope();

                        var fullInstance = scope.ServiceProvider.GetService(commandType) as ICommand;

                        if (fullInstance == default)
                        {
                            logger.LogCritical("Command {TypeName} could not be instantiated", commandType.FullName);
                            Environment.Exit(ExitCodes.CommandNotInstantiable);
                            return ExitCodes.CommandNotInstantiable;
                        }

                        await fullInstance.RunAsync(commandOptions);

                        return 0;
                    });
                });
            }
        }

        app.Execute(args);
    }
}
