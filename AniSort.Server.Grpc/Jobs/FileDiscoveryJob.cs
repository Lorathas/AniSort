using System.Collections.Immutable;
using AniSort.Core.Data;

namespace AniSort.Server.Jobs;

public record FileDiscoveryJob(Job Job, string Path, ImmutableList<FileProcessingJobState>? Files = null);
