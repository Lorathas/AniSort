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

    private readonly ILogger<SortCommand> log;

    private readonly IPathBuilderRepository pathBuilderRepository;

    private readonly IServiceProvider serviceProvider;

    private ConsoleProgressBar? hashProgressBar;

    public SortCommand(IConfigProvider configProvider, ILogger<SortCommand> log, AniDbClient client, IServiceProvider serviceProvider, IPathBuilderRepository pathBuilderRepository)
    {
        this.configProvider = configProvider;
        this.log = log;
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

            using (log.BeginScope("Config setup to write to following directories for files:"))
            {
                log.LogTrace("TV:     {TvPath}", Path.Combine(configProvider.Config.Destination.Path, configProvider.Config.Destination.TvPath));
                log.LogTrace("Movies: {MoviePath}", Path.Combine(configProvider.Config.Destination.Path, configProvider.Config.Destination.MoviePath));
                log.LogTrace("Path builder base path: {PathBuilderBasePath}", pathBuilderRepository.DefaultPathBuilder.Root);
            }
        }

        try
        {
            client.Connect();
            var auth = await client.AuthAsync();

            if (!auth.Success)
            {
                log.LogCritical("Invalid auth credentials. Unable to connect to AniDb");
                Environment.Exit(ExitCodes.InvalidAuthCredentials);
            }

            if (auth.HasNewVersion)
            {
                log.LogWarning("A new version of the software is available. Please download it when possible");
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
                first.Post(new Ok<BlockProvider.MetadataFileJobParams>(new BlockProvider.MetadataFileJobParams(new FileInfo(path))));
            }

            await last.Completion;

            updateHashBarCancellationSource.Cancel();
            if (updateHashBarTask != null)
            {
                await updateHashBarTask;
            }
            log.LogInformation("Finished processing {FileCount} files", fileCount);
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
    public ITargetBlock<Job> BuildPipeline(CancellationToken? cancellationToken = null)
    {
        return BuildPipelineInternal(cancellationToken).JobTarget;
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

    private (ITargetBlock<Job> JobTarget, ITargetBlock<Result<BlockProvider.MetadataFileJobParams>> FirstBlock, ITargetBlock<Result<BlockProvider.MetadataFileJobParams>> LastBlock) BuildPipelineInternal(CancellationToken? cancellationToken = null)
    {
        var blockProvider = serviceProvider.GetService<BlockProvider>() ?? throw new ApplicationException($"Unable to instantiate {typeof(BlockProvider)}");
        
        var blockOptions = new ExecutionDataflowBlockOptions { CancellationToken = cancellationToken ?? CancellationToken.None };

        var bufferBlock = new BufferBlock<Job>();

        var nullBlock = DataflowBlock.NullTarget<Result<BlockProvider.MetadataFileJobParams>>();
        
        var transformBlock = blockProvider.BuildSingleJobTransformBlock(blockOptions);
        var fetchFileBlock = blockProvider.BuildFetchLocalFileBlock(blockOptions);
        var hashFileBlock = blockProvider.BuildHashFileBlock(OnHashStarted, OnProgressUpdate, OnHashFinished, blockOptions);
        var filterCoolingDownFiles = blockProvider.BuildFilterCoolingDownFilesBlock(blockOptions);
        var searchFileBlock = blockProvider.BuildSearchFileBlock(client, blockOptions);
        var getVideoResolutionBlock = blockProvider.BuildGetFileVideoResolutionBlock(blockOptions);
        var renameBlock = blockProvider.BuildRenameFileBlock(blockOptions);

        var linkOptions = new DataflowLinkOptions { PropagateCompletion = true };

        bufferBlock.LinkTo(transformBlock, linkOptions);

        transformBlock.LinkTo(fetchFileBlock, linkOptions, Result<BlockProvider.MetadataFileJobParams>.IsOk);
        transformBlock.LinkTo(nullBlock);

        fetchFileBlock.LinkTo(hashFileBlock, linkOptions, r => r is Ok<BlockProvider.MetadataFileJobParams> && r.OkValue is {LocalFile: {Ed2kHash: null}});
        fetchFileBlock.LinkTo(filterCoolingDownFiles, linkOptions, Result<BlockProvider.MetadataFileJobParams>.IsOk);
        fetchFileBlock.LinkTo(nullBlock);

        filterCoolingDownFiles.LinkTo(searchFileBlock, linkOptions, r => r is Ok<BlockProvider.MetadataFileJobParams>);
        filterCoolingDownFiles.LinkTo(nullBlock);
        
        hashFileBlock.LinkTo(searchFileBlock, linkOptions, Result<BlockProvider.MetadataFileJobParams>.IsOk);
        hashFileBlock.LinkTo(nullBlock);

        searchFileBlock.LinkTo(getVideoResolutionBlock, linkOptions, r => r is Ok<BlockProvider.MetadataFileJobParams> && r.OkValue.VideoResolution == default);
        searchFileBlock.LinkTo(renameBlock, linkOptions, Result<BlockProvider.MetadataFileJobParams>.IsOk);
        searchFileBlock.LinkTo(nullBlock);
        getVideoResolutionBlock.LinkTo(renameBlock, linkOptions);

        return (transformBlock, fetchFileBlock, renameBlock);
    }
}
