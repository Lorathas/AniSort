using AniSort.Core.Data;
using AniSort.Core.Data.Repositories;
using AniSort.Server.Extensions;
using AniSort.Server.Hubs;
using Grpc.Core;

namespace AniSort.Server.Services;

public class LocalFileService : Server.LocalFileService.LocalFileServiceBase
{
    private readonly ILocalFileHub fileHub;

    private readonly ILocalFileRepository localFileRepository;

    private readonly ILogger<LocalFileService> logger;

    public LocalFileService(ILocalFileRepository localFileRepository, ILogger<LocalFileService> logger, ILocalFileHub fileHub)
    {
        this.localFileRepository = localFileRepository;
        this.logger = logger;
        this.fileHub = fileHub;
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
                { "LocalFileId", request.LocalFileId }
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
                { "LocalFileId", request.LocalFileId }
            });
        }

        return file.ToReply();
    }

    public override async Task<PagesInfo> GetPageInfo(FilteredLocalFilesRequest request, ServerCallContext context)
    {
        return new PagesInfo { Pages = await localFileRepository.CountSearchedFilesAsync(request.ToFilter()) };
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
        async Task SendDirectoryContents(string path)
        {
            string[] files = Directory.GetFiles(path);
            string[] directories = Directory.GetDirectories(path);

            var directoryFiles = new List<DirectoryFilesReply.Types.DirectoryFile>(files.Length + directories.Length);
            
            directoryFiles.AddRange(files.Select(f => new DirectoryFilesReply.Types.DirectoryFile{Name = f, Type = DirectoryFilesReply.Types.DirectoryFileType.File}));
            directoryFiles.AddRange(directories.Select(d => new DirectoryFilesReply.Types.DirectoryFile{Name = d, Type = DirectoryFilesReply.Types.DirectoryFileType.Directory}));

            var reply = new DirectoryFilesReply();
            reply.Files.AddRange(directoryFiles);

            await responseStream.WriteAsync(reply);
        }
        
        string homeFolder = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        await SendDirectoryContents(homeFolder);

        while (!context.CancellationToken.IsCancellationRequested)
        {
            await requestStream.MoveNext();

            if (context.CancellationToken.IsCancellationRequested) break;

            await SendDirectoryContents(requestStream.Current.Path);
        }
    }
    
    #endregion
}
