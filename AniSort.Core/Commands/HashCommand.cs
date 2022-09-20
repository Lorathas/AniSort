using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using AniSort.Core.Crypto;
using AniSort.Core.Data;
using AniSort.Core.DataFlow;
using AniSort.Core.Extensions;
using AniSort.Core.Helpers;
using Microsoft.Extensions.CommandLineUtils;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AniSort.Core.Commands;

public class HashCommand : IPipelineCommand
{
    private readonly Config config;

    private readonly ILogger<HashCommand> logger;

    private readonly IServiceProvider serviceProvider;

    private ConsoleProgressBar? hashProgressBar;

    public HashCommand(Config config, ILogger<HashCommand> logger, IServiceProvider serviceProvider)
    {
        this.config = config;
        this.logger = logger;
        this.serviceProvider = serviceProvider;
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

        while (fileQueue.TryDequeue(out var path))
        {
            await using var fs = new BufferedStream(File.OpenRead(path));
            var totalBytes = fs.Length;

            hashProgressBar = new ConsoleProgressBar(totalBytes, 40, postfixMessage: $"hashing: {path}",
                postfixMessageShort: $"hashing: {Path.GetFileName(path)}");

            var sw = Stopwatch.StartNew();

            var hashTask = Ed2k.HashMultiAsync(fs, new Progress<long>(OnProgressUpdate));

            while (!hashTask.IsCompleted)
            {
                if (EnvironmentHelpers.IsConsolePresent)
                {
                    hashProgressBar?.WriteNextFrame();
                }

                Thread.Sleep(TimeSpan.FromMilliseconds(100));
            }

            hashTask.Wait();

            var hash = hashTask.Result;

            hashProgressBar = null;

            sw.Stop();

            if (EnvironmentHelpers.IsConsolePresent)
            {
                logger.LogInformation(
                    $"\rHashed: {(path.Length + 8 > Console.WindowWidth ? Path.GetFileName(path) : path).Truncate(Console.WindowWidth)}");
            }
            else
            {
                logger.LogInformation("Hashed: {FilePath}", path);
            }

            logger.LogTrace($"  eD2k hash: {hash.ToHexString()}");

            logger.LogTrace(
                $"  Processed {(double) totalBytes / 1024 / 1024:###,###,##0.00}MB in {sw.Elapsed} at a rate of {Math.Round((double) totalBytes / 1024 / 1024 / sw.Elapsed.TotalSeconds):F2}MB/s");

            if (EnvironmentHelpers.IsConsolePresent)
            {
                Console.WriteLine();
            }
        }

        logger.LogInformation("Finished hashing all files. Exiting...");
    }

    private void OnProgressUpdate(long bytesProcessed)
    {
        if (hashProgressBar != null)
        {
            hashProgressBar.Progress = bytesProcessed;
        }
    }

    /// <inheritdoc />
    public IEnumerable<string> CommandNames => new[] { "hash" };

    /// <inheritdoc />
    public JobType[] Types => new[] { JobType.HashDirectory, JobType.HashFile };

    /// <inheritdoc />
    public string HelpOption => "-h --help";

    /// <inheritdoc />
    public bool IncludeCredentialOptions => false;

    /// <inheritdoc />
    public List<CommandOption> SetupCommand(CommandLineApplication command) => new();

    /// <inheritdoc />
    public ITargetBlock<Job> BuildPipeline()
    {
        var blockProvider = serviceProvider.GetService<BlockProvider>() ?? throw new ApplicationException($"Unable to instantiate {typeof(BlockProvider)}");

        var bufferBlock = new BufferBlock<BlockProvider.MetadataFileJobParams>();

        var jobTransformBlock = blockProvider.BuildJobTransformBlock(bufferBlock);

        var fetchFileBlock = blockProvider!.BuildFetchLocalFileBlock();
        var hashFileBlock = blockProvider.BuildHashFileBlock((_, _) =>
        {
        }, OnProgressUpdate, () =>
        {
        });

        var options = new DataflowLinkOptions { PropagateCompletion = true };

        bufferBlock.LinkTo(fetchFileBlock, options);

        fetchFileBlock.LinkTo(hashFileBlock, options, f => f.LocalFile is { Ed2kHash: null });
        fetchFileBlock.LinkTo(DataflowBlock.NullTarget<BlockProvider.MetadataFileJobParams>());

        hashFileBlock.LinkTo(DataflowBlock.NullTarget<BlockProvider.MetadataFileJobParams>());

        return jobTransformBlock;
    }
}
