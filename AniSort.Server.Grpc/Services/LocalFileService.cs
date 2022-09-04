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

    public override async Task ListenForFileUpdates(LocalFileRequest request, IServerStreamWriter<LocalFileReply> responseStream, ServerCallContext context)
    {
        async Task Listener(LocalFile file, HubUpdate update)
        {
            await responseStream.WriteAsync(file.ToReply());
        }

        var job = await localFileRepository.GetByIdWithRelatedAsync(Guid.Parse(request.LocalFileId));

        if (job == null)
        {
            throw new RpcException(new Status(StatusCode.NotFound, $"No local file found for id {request.LocalFileId}"), new Metadata
            {
                { "LocalFileId", request.LocalFileId }
            });
        }

        await Listener(job, HubUpdate.Initial);

        await fileHub.RegisterListenerAsync(Listener, context.CancellationToken);

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
}
