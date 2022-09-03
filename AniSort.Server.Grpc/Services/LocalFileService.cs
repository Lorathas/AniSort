using AniSort.Core.Data.Repositories;
using Grpc.Core;

namespace AniSort.Server.Services;

public class LocalFileService : Server.LocalFileService.LocalFileServiceBase
{
    private readonly ILocalFileRepository localFileRepository;
    
    public override Task ListFiles(FilteredLocalFilesRequest request, IServerStreamWriter<LocalFile> responseStream, ServerCallContext context)
    {
        
        
        return base.ListFiles(request, responseStream, context);
    }

    public override Task ListenForFileUpdates(FilteredLocalFilesRequest request, IServerStreamWriter<LocalFile> responseStream,
        ServerCallContext context)
    {
        return base.ListenForFileUpdates(request, responseStream, context);
    }

    public override Task<LocalFile> GetFileDetails(LocalFileRequest request, ServerCallContext context)
    {
        return base.GetFileDetails(request, context);
    }
}