using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.CommandLineUtils;

namespace AniSort.Core.Commands;

public interface ICommand
{
    Task RunAsync(List<CommandOption> commandOptions);

    IEnumerable<string> CommandNames { get; }
    
    string HelpOption { get; }
    
    bool IncludeCredentialOptions { get; }

    List<CommandOption> SetupCommand(CommandLineApplication command);
}
