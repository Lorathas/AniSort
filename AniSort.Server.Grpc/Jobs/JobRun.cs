using AniSort.Core.Data;

namespace AniSort.Server.Jobs;

public record JobRun(Job Job, CancellationToken CancellationToken);
