using System.Threading.Tasks.Dataflow;
using AniSort.Core.Data;

namespace AniSort.Core.Commands;

public interface IPipelineCommand : ICommand
{
    ITargetBlock<Job> BuildPipeline();
}
