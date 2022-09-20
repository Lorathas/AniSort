using System.Collections.Generic;
using System.Threading.Tasks;
using AniSort.Core.Data;
using Microsoft.Extensions.CommandLineUtils;

namespace AniSort.Core.Commands;

public interface ICommand
{
    IEnumerable<string> CommandNames { get; }
    
    JobType[] Types { get; }

    string HelpOption { get; }

    bool IncludeCredentialOptions { get; }

    Task RunAsync(List<CommandOption> commandOptions);

    List<CommandOption> SetupCommand(CommandLineApplication command);
}
