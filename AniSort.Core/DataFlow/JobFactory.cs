using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AniSort.Core.Commands;
using AniSort.Core.Data;
using AniSort.Core.Extensions;
using Google.Protobuf.WellKnownTypes;

namespace AniSort.Core.DataFlow;

public class JobFactory : IJobFactory
{
    private static void AddStepsToSortJob(Job job)
    {
        job.Steps.Add(new JobStep { Name = "Discover Files", Type = StepType.DiscoverFiles, TotalProgress = 1});

        job.Steps.Add(new JobStep { Name = "Fetch File Info", Type = StepType.FetchLocalFile });

        job.Steps.Add(new JobStep { Name = "Hash File", Type = StepType.Hash });

        job.Steps.Add(new JobStep { Name = "Fetch Metadata", Type = StepType.FetchMetadata });

        job.Steps.Add(new JobStep { Name = "Rename File", Type = StepType.Sort });
    }

    /// <inheritdoc />
    public IEnumerable<Job> CreateSortDirectoryJobs(string name, string path) => PathExtensions.WalkDirectoryFiles(path, (p) => FileImportExtensions.SupportedFileExtensions.Contains(Path.GetExtension(p))).Select(file => CreateSortFileJob(name, file));

    /// <inheritdoc />
    public Job CreateSortFileJob(string name, string path)
    {
        var options = new Struct();

        options.Fields[JobData.Path].StringValue = path;

        var job = new Job
        {
            Name = name, Type = JobType.SortFile, Options = options, QueuedAt = DateTimeOffset.Now,
        };

        AddStepsToSortJob(job);

        return job;
    }

    private static void AddStepsToHashJob(Job job)
    {
        job.Steps.Add(new JobStep { Name = "Discover Files", Type = StepType.DiscoverFiles });

        job.Steps.Add(new JobStep { Name = "Fetch File Info", Type = StepType.FetchLocalFile });

        job.Steps.Add(new JobStep { Name = "Hash File", Type = StepType.Hash });
    }

    /// <inheritdoc />
    public IEnumerable<Job> CreateHashDirectoryJobs(string name, string path) => PathExtensions.WalkDirectoryFiles(path, p => FileImportExtensions.SupportedFileExtensions.Contains(Path.GetExtension(p))).Select(file => CreateHashFileJob(name, file));

    /// <inheritdoc />
    public Job CreateHashFileJob(string name, string path)
    {
        var options = new Struct();

        options.Fields[JobData.Path].StringValue = path;

        var job = new Job
        {
            Name = name, Type = JobType.HashFile, Options = options, QueuedAt = DateTimeOffset.Now,
        };

        AddStepsToHashJob(job);

        return job;
    }
}
