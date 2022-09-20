using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using System.Windows.Input;
using AniDbSharp;
using AniDbSharp.Data;
using AniDbSharp.Exceptions;
using AniSort.Core.Crypto;
using AniSort.Core.Data;
using AniSort.Core.Data.Repositories;
using AniSort.Core.DataFlow;
using AniSort.Core.Exceptions;
using AniSort.Core.Extensions;
using AniSort.Core.Helpers;
using AniSort.Core.IO;
using AniSort.Core.Models;
using AniSort.Core.Utils;
using FFMpegCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.CommandLineUtils;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using FileInfo = AniDbSharp.Data.FileInfo;

namespace AniSort.Core.Commands;

public class SortCommand : IPipelineCommand
{
    private readonly Config config;
    private readonly ILogger<SortCommand> logger;
    private readonly AniDbClient client;
    private readonly IServiceProvider serviceProvider;
    private readonly IPathBuilderRepository pathBuilderRepository;
    private ConsoleProgressBar? hashProgressBar;

    public SortCommand(Config config, ILogger<SortCommand> logger, AniDbClient client, IServiceProvider serviceProvider, IPathBuilderRepository pathBuilderRepository)
    {
        this.config = config;
        this.logger = logger;
        this.client = client;
        this.serviceProvider = serviceProvider;
        this.pathBuilderRepository = pathBuilderRepository;
    }

    /// <inheritdoc />
    public async Task RunAsync(List<CommandOption> commandOptions)
    {
        var fileQueue = new Queue<string>();

        fileQueue.AddPathsToQueue(config.Sources);
        if (!config.IgnoreLibraryFiles)
        {
            fileQueue.AddPathsToQueue(config.LibraryPaths);
        }

        if (config.Verbose)
        {
            if (EnvironmentHelpers.IsConsolePresent)
            {
                Console.WriteLine();
            }

            using (logger.BeginScope("Config setup to write to following directories for files:"))
            {
                logger.LogTrace("TV:     {TvPath}", Path.Combine(config.Destination.Path, config.Destination.TvPath));
                logger.LogTrace("Movies: {MoviePath}", Path.Combine(config.Destination.Path, config.Destination.MoviePath));
                logger.LogTrace("Path builder base path: {PathBuilderBasePath}", pathBuilderRepository.DefaultPathBuilder.Root);
            }
        }

        try
        {
            client.Connect();
            var auth = await client.AuthAsync();

            if (!auth.Success)
            {
                logger.LogCritical("Invalid auth credentials. Unable to connect to AniDb");
                Environment.Exit(ExitCodes.InvalidAuthCredentials);
            }

            if (auth.HasNewVersion)
            {
                logger.LogWarning("A new version of the software is available. Please download it when possible");
            }
            
            var updateHashBarCancellationSource = new CancellationTokenSource();
            Task updateHashBarTask = null;

            if (EnvironmentHelpers.IsConsolePresent)
            {
                // ReSharper disable once MethodSupportsCancellation
                updateHashBarTask = Task.Run(async () =>
                {
                    var cancellationToken = updateHashBarCancellationSource.Token;

                    while (!cancellationToken.IsCancellationRequested)
                    {
                        hashProgressBar?.WriteNextFrame();

                        // ReSharper disable once MethodSupportsCancellation
                        await Task.Delay(TimeSpan.FromMilliseconds(100));
                    }
                });
            }

            var (_, first, last) = BuildPipelineInternal();

            var queue = new Queue<string>();

            queue.AddPathsToQueue(config.Sources);
            if (!config.IgnoreLibraryFiles)
            {
                queue.AddPathsToQueue(config.LibraryPaths);
            }

            int fileCount = queue.Count;

            while (queue.TryDequeue(out string path))
            {
                first.Post(new(new System.IO.FileInfo(path)));
            }

            await last.Completion;

            updateHashBarCancellationSource.Cancel();
            if (updateHashBarTask != null)
            {
                await updateHashBarTask;
            }
            logger.LogInformation("Finished processing {FileCount} files", fileCount);
        }
        finally
        {
            await client.DisposeAsync();
        }
    }

    private void OnHashStarted(string path, long totalBytes)
    {
        if (EnvironmentHelpers.IsConsolePresent)
        {
            hashProgressBar = new ConsoleProgressBar(totalBytes, 40, postfixMessage: $"hashing: {path}",
                postfixMessageShort: $"hashing: {Path.GetFileName(path)}");
        }
    }

    private void OnProgressUpdate(long bytesProcessed)
    {
        if (hashProgressBar != null)
        {
            hashProgressBar.Progress = bytesProcessed;
        }
    }

    private void OnHashFinished()
    {
        if (!EnvironmentHelpers.IsConsolePresent)
        {
            return;
        }
        if (hashProgressBar != null)
        {
            hashProgressBar.Progress = hashProgressBar.TotalProgress;
            hashProgressBar.WriteNextFrame();
            hashProgressBar = null;
        }
        Console.WriteLine();
    }

    /// <inheritdoc />
    public IEnumerable<string> CommandNames => new[] { "sort" };

    /// <inheritdoc />
    public JobType[] Types => new [] { JobType.SortDirectory, JobType.SortFile };

    /// <inheritdoc />
    public string HelpOption => "-h --help";

    /// <inheritdoc />
    public bool IncludeCredentialOptions => true;

    /// <inheritdoc />
    public List<CommandOption> SetupCommand(CommandLineApplication command) => new();

    private (ITargetBlock<Job> JobTarget, ITargetBlock<BlockProvider.MetadataFileJobParams> FirstBlock, ITargetBlock<BlockProvider.MetadataFileJobParams> LastBlock) BuildPipelineInternal()
    {
        var blockProvider = serviceProvider.GetService<BlockProvider>() ?? throw new ApplicationException($"Unable to instantiate {typeof(BlockProvider)}");

        var bufferBlock = new BufferBlock<BlockProvider.MetadataFileJobParams>();

        var jobTarget = blockProvider.BuildJobTransformBlock(bufferBlock);

        var fetchFileBlock = blockProvider!.BuildFetchLocalFileBlock();
        var hashFileBlock = blockProvider.BuildHashFileBlock(OnHashStarted, OnProgressUpdate, OnHashFinished);
        var filterCoolingDownFiles = blockProvider.BuildFilterCoolingDownFilesBlock();
        var searchFileBlock = blockProvider.BuildSearchFileBlock(client);
        var getVideoResolutionBlock = blockProvider.BuildGetFileVideoResolutionBlock();
        var renameBlock = blockProvider.BuildRenameFileBlock();

        var options = new DataflowLinkOptions { PropagateCompletion = true };
        
        bufferBlock.LinkTo(fetchFileBlock, options);

        fetchFileBlock.LinkTo(hashFileBlock, options, f => f.LocalFile is { Ed2kHash: null });
        fetchFileBlock.LinkTo(filterCoolingDownFiles, options, f => !f.Failed);
        fetchFileBlock.LinkTo(DataflowBlock.NullTarget<BlockProvider.MetadataFileJobParams>());

        filterCoolingDownFiles.LinkTo(searchFileBlock, options, f => !f.IsCoolingDown);
        filterCoolingDownFiles.LinkTo(DataflowBlock.NullTarget<BlockProvider.MetadataFileJobParams>());
        hashFileBlock.LinkTo(searchFileBlock, options, f => !f.Failed);
        hashFileBlock.LinkTo(DataflowBlock.NullTarget<BlockProvider.MetadataFileJobParams>());

        searchFileBlock.LinkTo(getVideoResolutionBlock, options, a => a.VideoResolution == default);
        searchFileBlock.LinkTo(renameBlock, options, t => t.Failed);
        searchFileBlock.LinkTo(DataflowBlock.NullTarget<BlockProvider.MetadataFileJobParams>());
        getVideoResolutionBlock.LinkTo(renameBlock, options);

        return (jobTarget, bufferBlock, renameBlock);
    }
    
    /// <inheritdoc />
    public ITargetBlock<Job> BuildPipeline()
    {
        return BuildPipelineInternal().JobTarget;
    }
}
