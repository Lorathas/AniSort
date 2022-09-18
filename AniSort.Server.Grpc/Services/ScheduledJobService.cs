using AniSort.Core.Data.Repositories;
using AniSort.Server.Extensions;
using AniSort.Server.Hubs;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.AspNetCore.SignalR;

namespace AniSort.Server.Services;

public class ScheduledJobService : Server.ScheduledJobService.ScheduledJobServiceBase
{
    private readonly ILogger<ScheduledJobService> logger;

    private readonly IScheduledJobRepository scheduledJobRepository;

    private readonly IScheduledJobHub scheduledJobHub;

    /// <inheritdoc />
    public ScheduledJobService(ILogger<ScheduledJobService> logger, IScheduledJobRepository scheduledJobRepository, IScheduledJobHub scheduledJobHub)
    {
        this.logger = logger;
        this.scheduledJobRepository = scheduledJobRepository;
        this.scheduledJobHub = scheduledJobHub;
    }

    /// <inheritdoc />
    public override async Task ListScheduledJobs(Google.Protobuf.WellKnownTypes.Empty request, IServerStreamWriter<ScheduledJob> responseStream, ServerCallContext context)
    {
        var scheduledJobs = scheduledJobRepository.GetAllOrderedByName();

        await scheduledJobs
            .Select(job => job.ToReply())
            .ForEachAwaitAsync(async r => await responseStream.WriteAsync(r));
    }

    /// <inheritdoc />
    public override async Task<ScheduledJob> CreateScheduledJob(ScheduledJob request, ServerCallContext context)
    {
        var created = await scheduledJobRepository.AddAsync(new Core.Data.ScheduledJob
        {
            Name = request.Name,
            Type = (Core.Data.JobType) request.Type,
            ScheduleType = (Core.Data.ScheduleType) request.ScheduleType,
            Options = request.Options,
            ScheduleOptions = request.ScheduleOptions,
        });
        await scheduledJobRepository.SaveChangesAsync();

        await scheduledJobHub.PublishUpdateAsync(created, HubUpdate.ItemCreated);

        return created.ToReply();
    }

    /// <inheritdoc />
    public override async Task<ScheduledJob> UpdateScheduledJob(ScheduledJob request, ServerCallContext context)
    {
        if (!request.HasScheduledJobId)
        {
            throw new RpcException(new Status(StatusCode.NotFound, "Cannot update scheduled jobs without ScheduledJobId"), Metadata.Empty);
        }

        var existing = await scheduledJobRepository.GetByIdAsync(new Guid(request.ScheduledJobId));

        existing.Name = request.Name;
        existing.Type = (Core.Data.JobType) request.Type;
        existing.ScheduleType = (Core.Data.ScheduleType) request.ScheduleType;
        existing.Options = request.Options;
        existing.ScheduleOptions = request.ScheduleOptions;

        await scheduledJobRepository.SaveChangesAsync();

        await scheduledJobHub.PublishUpdateAsync(existing, HubUpdate.ItemUpdated);

        return existing.ToReply();
    }

    /// <inheritdoc />
    public override async Task ListenForScheduledJobUpdates(ScheduledJobFilterRequest request, IServerStreamWriter<ScheduledJobUpdate> responseStream, ServerCallContext context)
    {
        Guid scheduledJobId = new Guid(request.ScheduledJobId);

        if (!await scheduledJobRepository.ExistsAsync(scheduledJobId))
        {
            throw new RpcException(new Status(StatusCode.NotFound, $"No scheduled job found with id {scheduledJobId}"), new Metadata
            {
                {"ScheduledJobId", scheduledJobId.ToString()}
            });
        }
        
        await scheduledJobHub.RegisterListenerAsync(scheduledJobId, async (job, update) =>
        {
            var reply = job.ToReply(update);
            
            await responseStream.WriteAsync(reply);
        }, context.CancellationToken);
    }

    /// <inheritdoc />
    public override async Task ListenForListOfScheduledJobs(Empty request, IServerStreamWriter<ScheduledJobCollection> responseStream, ServerCallContext context)
    {
        async Task SendScheduledJobsAsync()
        {
            var reply = new ScheduledJobCollection();
            
            await foreach (var scheduledJob in scheduledJobRepository.GetAllOrderedByName())
            {
                reply.Jobs.Add(scheduledJob.ToReply());
            }

            await responseStream.WriteAsync(reply);
        }

        await SendScheduledJobsAsync();
        
        await scheduledJobHub.RegisterListenerAsync(async (_, _) => await SendScheduledJobsAsync(), context.CancellationToken);

        await context.CancellationToken;
    }
}
