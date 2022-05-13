using System.Collections.Generic;
using System.Threading.Tasks;

namespace AniSort.Core.Commands;

public interface ICommand
{
    Task RunAsync();

    IEnumerable<string> CommandName { get; }
}
