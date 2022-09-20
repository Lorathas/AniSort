using AniSort.Core.Data;
using AniSort.Core.Models;
using FileInfo = System.IO.FileInfo;

namespace AniSort.Server.Jobs;

public record FileProcessingJobState(Job Job, string Path, FileInfo Info, Core.Models.ImportStatus ImportStatus);
