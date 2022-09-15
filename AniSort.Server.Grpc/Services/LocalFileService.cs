using System.Diagnostics;
using System.Runtime.Caching;
using AniSort.Core.Data;
using AniSort.Core.Data.Repositories;
using AniSort.Server.Extensions;
using AniSort.Server.Hubs;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;

namespace AniSort.Server.Services;

public class LocalFileService : Server.LocalFileService.LocalFileServiceBase
{
    private readonly ILocalFileHub fileHub;

    private readonly ILocalFileRepository localFileRepository;

    private readonly ILogger<LocalFileService> logger;

    private readonly ActivitySource activitySource;

    private readonly MemoryCache directoryContentsCache = new("DirectoryContents");

    private readonly MemoryCache directorySubdirectoriesCache = new("DirectorySubdirectories");

    public LocalFileService(ILocalFileRepository localFileRepository, ILogger<LocalFileService> logger, ILocalFileHub fileHub, ActivitySource activitySource)
    {
        this.localFileRepository = localFileRepository;
        this.logger = logger;
        this.fileHub = fileHub;
        this.activitySource = activitySource;
    }

    public override async Task ListFiles(FilteredLocalFilesRequest request, IServerStreamWriter<LocalFileReply> responseStream, ServerCallContext context)
    {
        var localFiles = localFileRepository.SearchForFilesPagedAsync(request.ToFilter(), ServerConstants.PageSize);

        await localFiles
            .Select(job => job.ToReply(false, false))
            .ForEachAwaitAsync(async r => await responseStream.WriteAsync(r));
    }

    public override async Task ListenForFileUpdates(LocalFileRequest request, IServerStreamWriter<LocalFileUpdateReply> responseStream, ServerCallContext context)
    {
        logger.LogTrace("Listener for updates registered at ");

        var localFileId = new Guid(request.LocalFileId);

        async Task Listener(LocalFile file, HubUpdate update)
        {
            await responseStream.WriteAsync(file.ToUpdateReply(update));
        }

        var file = await localFileRepository.GetByIdWithRelatedAsync(localFileId);

        if (file == null)
        {
            throw new RpcException(new Status(StatusCode.NotFound, $"No local file found for id {request.LocalFileId}"), new Metadata
            {
                {
                    "LocalFileId", request.LocalFileId
                }
            });
        }

        await Listener(file, HubUpdate.Initial);

        await fileHub.RegisterListenerAsync(localFileId, Listener, context.CancellationToken);

        await context.CancellationToken;
    }

    /// <inheritdoc />
    public override async Task ListenForAllFileUpdates(FilteredLocalFilesRequest request, IServerStreamWriter<LocalFileUpdateReply> responseStream, ServerCallContext context)
    {
        var filter = request.ToFilter();

        async Task Listener(LocalFile file, HubUpdate update)
        {
            await responseStream.WriteAsync(file.ToUpdateReply(update));
        }

        await fileHub.RegisterListenerAsync(filter.Matches, Listener, context.CancellationToken);

        await context.CancellationToken;
    }

    public override async Task<LocalFileReply> GetFileDetails(LocalFileRequest request, ServerCallContext context)
    {
        var file = await localFileRepository.GetByIdWithRelatedAsync(new Guid(request.LocalFileId));

        if (file == null)
        {
            throw new RpcException(new Status(StatusCode.NotFound, $"No local file found for id {request.LocalFileId}"), new Metadata
            {
                {
                    "LocalFileId", request.LocalFileId
                }
            });
        }

        return file.ToReply();
    }

    public override async Task<PagesInfo> GetPageInfo(FilteredLocalFilesRequest request, ServerCallContext context)
    {
        return new PagesInfo
        {
            Pages = await localFileRepository.CountSearchedFilesAsync(request.ToFilter())
        };
    }

    #region File Finding

    /// <inheritdoc />
    public override Task<DrivesReply> GetDrives(Empty request, ServerCallContext context)
    {
        var reply = new DrivesReply();

        reply.Drives.AddRange(Directory.GetLogicalDrives());

        return Task.FromResult(reply);
    }

    /// <inheritdoc />
    public override async Task GetFilesInDirectory(IAsyncStreamReader<DirectoryFilesRequest> requestStream, IServerStreamWriter<DirectoryFilesReply> responseStream, ServerCallContext context)
    {
        Func<string, List<DirectoryFilesReply.Types.DirectoryFile>> GetDirectoryContents(bool excludeFiles)
        {
            return path =>
            {
                using var activity = activitySource?.StartActivity();
                activity?.AddBaggage(nameof(path), path);
                activity?.AddBaggage(nameof(excludeFiles), excludeFiles.ToString());
                
                string[]? files = null;
                string[] subdirectories;
                try
                {
                    if (!excludeFiles)
                    {
                        activity?.AddEvent(new("Get Files"));
                        files = Directory.GetFiles(path);
                    }
                    activity?.AddEvent(new("Get Subdirectories"));
                    subdirectories = Directory.GetDirectories(path);
                }
                catch (UnauthorizedAccessException)
                {
                    string errorMessage = $"AniSort does not have permission to access the path {path}";
                    activity?.SetStatus(ActivityStatusCode.Error, errorMessage);
                    throw new RpcException(new Status(StatusCode.PermissionDenied, errorMessage), new Metadata
                    {
                        {
                            "Path", path
                        }
                    });
                }

                var directoryFiles = new List<DirectoryFilesReply.Types.DirectoryFile>(files?.Length ?? 0 + subdirectories.Length);

                activity?.AddEvent(new("Build Response Collection"));
                if (files != null)
                {
                    directoryFiles.AddRange(files.Select(f => new DirectoryFilesReply.Types.DirectoryFile
                    {
                        Name = Path.GetFileName(f),
                        Path = f,
                        Type = DirectoryFilesReply.Types.DirectoryFileType.File
                    }));
                }
                directoryFiles.AddRange(subdirectories.Select(d => new DirectoryFilesReply.Types.DirectoryFile
                {
                    Name = Path.GetFileName(d),
                    Path = d,
                    Type = DirectoryFilesReply.Types.DirectoryFileType.Directory
                }));

                return directoryFiles;
            };
        }

        async Task SendDirectoryContents(DirectoryFilesRequest request)
        {
            using var activity = activitySource.StartActivity();
            activity?.AddBaggage(nameof(request.Path), request.Path);
            activity?.AddBaggage(nameof(request.IncludeDrives), request.IncludeDrives.ToString());
            activity?.AddBaggage(nameof(request.ExcludeFiles), request.ExcludeFiles.ToString());

            activity?.AddEvent(new("Fetch Directory Contents"));
            var directoryFiles = (request.ExcludeFiles ? directorySubdirectoriesCache : directoryContentsCache).GetOrFetch(request.Path, new CacheItemPolicy
            {
                AbsoluteExpiration = DateTimeOffset.Now.AddMinutes(1)
            }, GetDirectoryContents(request.ExcludeFiles));

            var reply = new DirectoryFilesReply
            {
                CurrentPath = request.Path
            };
            reply.Files.AddRange(directoryFiles);

            if (request.IncludeDrives)
            {
                reply.Drives.AddRange(Directory.GetLogicalDrives());
            }

            activity?.AddEvent(new("Write Directory Contents"));
            await responseStream.WriteAsync(reply);
        }

        string homeFolder = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        await SendDirectoryContents(new DirectoryFilesRequest
        {
            Path = homeFolder,
            IncludeDrives = true,
        });

        while (!context.CancellationToken.IsCancellationRequested)
        {
            await requestStream.MoveNext(context.CancellationToken);

            if (context.CancellationToken.IsCancellationRequested) break;

            await SendDirectoryContents(requestStream.Current);
        }
    }

    #endregion
}
