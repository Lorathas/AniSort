using AniSort.Core.Data;

namespace AniSort.Server.Jobs;

public class JobRun
{
    public Job Job { get; init; }
    public CancellationToken CancellationToken { get; init; }
}
