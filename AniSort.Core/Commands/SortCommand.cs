using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using AniDbSharp;
using AniSort.Core.Data;
using AniSort.Core.DataFlow;
using AniSort.Core.Extensions;
using AniSort.Core.Helpers;
using AniSort.Core.IO;
using Microsoft.Extensions.CommandLineUtils;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AniSort.Core.Commands;

// ReSharper disable once ClassNeverInstantiated.Global
public class SortCommand : IPipelineCommand
{
    private readonly AniDbClient client;

    private readonly IConfigProvider configProvider;

    private readonly ILogger<SortCommand> logger;

    private readonly IPathBuilderRepository pathBuilderRepository;

    private readonly IServiceProvider serviceProvider;

    private ConsoleProgressBar? hashProgressBar;

    public SortCommand(IConfigProvider configProvider, ILogger<SortCommand> logger, AniDbClient client, IServiceProvider serviceProvider, IPathBuilderRepository pathBuilderRepository)
    {
        this.configProvider = configProvider;
        this.logger = logger;
        this.client = client;
        this.serviceProvider = serviceProvider;
        this.pathBuilderRepository = pathBuilderRepository;
    }

    /// <inheritdoc />
    public async Task RunAsync(List<CommandOption> commandOptions)
    {
        var fileQueue = new Queue<string>();

        fileQueue.AddPathsToQueue(configProvider.Config.Sources);
        if (!configProvider.Config.IgnoreLibraryFiles)
        {
            fileQueue.AddPathsToQueue(configProvider.Config.LibraryPaths);
        }

        if (configProvider.Config.Verbose)
        {
            if (EnvironmentHelpers.IsConsolePresent)
            {
                Console.WriteLine();
            }

            using (logger.BeginScope("Config setup to write to following directories for files:"))
            {
                logger.LogTrace("TV:     {TvPath}", Path.Combine(configProvider.Config.Destination.Path, configProvider.Config.Destination.TvPath));
                logger.LogTrace("Movies: {MoviePath}", Path.Combine(configProvider.Config.Destination.Path, configProvider.Config.Destination.MoviePath));
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
            Task? updateHashBarTask = null;

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

            queue.AddPathsToQueue(configProvider.Config.Sources);
            if (!configProvider.Config.IgnoreLibraryFiles)
            {
                queue.AddPathsToQueue(configProvider.Config.LibraryPaths);
            }

            int fileCount = queue.Count;

            while (queue.TryDequeue(out string? path))
            {
                first.Post(new BlockProvider.MetadataFileJobParams(new FileInfo(path)));
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

    /// <inheritdoc />
    public IEnumerable<string> CommandNames => new[] { "sort" };

    /// <inheritdoc />
    public JobType[] Types => new[] { JobType.SortDirectory, JobType.SortFile };

    /// <inheritdoc />
    public string HelpOption => "-h --help";

    /// <inheritdoc />
    public bool IncludeCredentialOptions => true;

    /// <inheritdoc />
    public List<CommandOption> SetupCommand(CommandLineApplication command)
    {
        return new();
    }

    /// <inheritdoc />
    public ITargetBlock<Job> BuildPipeline()
    {
        return BuildPipelineInternal().JobTarget;
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

    private (ITargetBlock<Job> JobTarget, ITargetBlock<BlockProvider.MetadataFileJobParams> FirstBlock, ITargetBlock<BlockProvider.MetadataFileJobParams> LastBlock) BuildPipelineInternal()
    {
        var blockProvider = serviceProvider.GetService<BlockProvider>() ?? throw new ApplicationException($"Unable to instantiate {typeof(BlockProvider)}");

        var bufferBlock = new BufferBlock<BlockProvider.MetadataFileJobParams>();

        var jobTarget = blockProvider.BuildJobTransformBlock(bufferBlock);

        var fetchFileBlock = blockProvider.BuildFetchLocalFileBlock();
        var hashFileBlock = blockProvider.BuildHashFileBlock(OnHashStarted, OnProgressUpdate, OnHashFinished);
        var filterCoolingDownFiles = blockProvider.BuildFilterCoolingDownFilesBlock();
        var searchFileBlock = blockProvider.BuildSearchFileBlock(client);
        var getVideoResolutionBlock = blockProvider.BuildGetFileVideoResolutionBlock();
        var renameBlock = blockProvider.BuildRenameFileBlock();

        var options = new DataflowLinkOptions { PropagateCompletion = true };

        bufferBlock.LinkTo(fetchFileBlock, options);

        fetchFileBlock.LinkTo(hashFileBlock, options, f => !f.Failed && f.LocalFile is { Ed2kHash: null });
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
}
