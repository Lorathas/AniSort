using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using AniDbSharp;
using AniSort.Core.DataFlow;
using AniSort.Core.Extensions;
using AniSort.Core.Helpers;
using AniSort.Core.IO;
using Microsoft.Extensions.CommandLineUtils;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AniSort.Core.Commands;

public class SortCommand : ICommand
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


            var blockProvider = serviceProvider.GetService<BlockProvider>();

            var bufferBlock = new BufferBlock<string>();

            var fetchFileBlock = blockProvider!.BuildFetchLocalFileBlock();
            var hashFileBlock = blockProvider.BuildHashFileBlock(OnHashStarted, OnProgressUpdate, OnHashFinished);
            var filterCoolingDownFiles = blockProvider.BuildFilterCoolingDownFilesBlock();
            var searchFileBlock = blockProvider.BuildSearchFileBlock(client);
            var getVideoResolutionBlock = blockProvider.BuildGetFileVideoResolutionBlock();
            var renameBlock = blockProvider.BuildRenameFileBlock();

            var nullTarget = DataflowBlock.NullTarget<FileImport>();

            bufferBlock.LinkTo(fetchFileBlock, new DataflowLinkOptions { PropagateCompletion = true });

            fetchFileBlock.LinkTo(hashFileBlock, new DataflowLinkOptions { PropagateCompletion = true }, f => f.State == FileImportState.Nominal && f.LocalFile.Ed2kHash == null);
            fetchFileBlock.LinkTo(filterCoolingDownFiles, new DataflowLinkOptions { PropagateCompletion = true }, f => f.State == FileImportState.Nominal);
            fetchFileBlock.LinkTo(nullTarget);

            filterCoolingDownFiles.LinkTo(searchFileBlock, f => f.State == FileImportState.Nominal);
            filterCoolingDownFiles.LinkTo(nullTarget);
            hashFileBlock.LinkTo(searchFileBlock, f => f.State == FileImportState.Nominal);
            hashFileBlock.LinkTo(nullTarget);

            searchFileBlock.LinkTo(getVideoResolutionBlock, f => f is { State: FileImportState.Nominal, Resolution: null });
            searchFileBlock.LinkTo(renameBlock, f => f.State == FileImportState.Nominal);
            searchFileBlock.LinkTo(nullTarget);
            getVideoResolutionBlock.LinkTo(renameBlock);

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

            var queue = new Queue<string>();

            queue.AddPathsToQueue(config.Sources);
            if (!config.IgnoreLibraryFiles)
            {
                queue.AddPathsToQueue(config.LibraryPaths);
            }

            int fileCount = queue.Count;

            while (queue.TryDequeue(out string? path))
            {
                bufferBlock.Post(path);
            }

            bufferBlock.Complete();
            await bufferBlock.Completion;
            fetchFileBlock.Complete();
            await fetchFileBlock.Completion;
            filterCoolingDownFiles.Complete();
            hashFileBlock.Complete();
            await Task.WhenAll(filterCoolingDownFiles.Completion, hashFileBlock.Completion);
            searchFileBlock.Complete();
            await searchFileBlock.Completion;
            getVideoResolutionBlock.Complete();
            await getVideoResolutionBlock.Completion;
            renameBlock.Complete();
            await renameBlock.Completion;

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
    public string HelpOption => "-h --help";

    /// <inheritdoc />
    public bool IncludeCredentialOptions => true;

    /// <inheritdoc />
    public List<CommandOption> SetupCommand(CommandLineApplication command) => new();

}
