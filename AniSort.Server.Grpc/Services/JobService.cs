using System.Threading.Tasks.Dataflow;
using AniSort.Core.Data;
using AniSort.Core.Data.Repositories;
using Grpc.Core;
using AniSort.Server;
using AniSort.Server.Extensions;
using AniSort.Server.Hubs;
using Google.Protobuf.Collections;
using Google.Protobuf.WellKnownTypes;

namespace AniSort.Server.Services;

public class JobService : Server.JobService.JobServiceBase
{
    private readonly ILogger<JobService> logger;
    private readonly IJobHub jobHub;
    private readonly IJobRepository jobRepository;

    public JobService(ILogger<JobService> logger, IJobHub jobHub, IJobRepository jobRepository)
    {
        this.logger = logger;
        this.jobHub = jobHub;
        this.jobRepository = jobRepository;
    }

    public override async Task ListenForJobChanges(FilteredJobsRequest request,
        IServerStreamWriter<JobUpdateReply> responseStream, ServerCallContext context)
    {
        await jobHub.RegisterListenerAsync(async (job, update) =>
        {
            var reply = new JobUpdateReply
            {
                JobId = job.Id.ToString(),
                Name = job.Name,
                Type = (JobType)job.Type,
                Status = (JobStatus)job.Status,
                PercentComplete = job.PercentComplete,
                IsFinished = job.IsFinished,
                UpdateType = (JobUpdate) update,
                StartedAt = job.StartedAt?.ToTimestamp(),
                CompletedAt = job.CompletedAt?.ToTimestamp()
            };
            reply.Steps.AddRange(job.Steps.Select(s => new JobStep
            {
                StepId = s.Id.ToString(),
                Name = s.Name,
                Status = (JobStatus)s.Status,
                PercentComplete = s.PercentComplete,
                StartedAt = s.StartedAt?.ToTimestamp(),
                CompletedAt = s.CompletedAt?.ToTimestamp(),
            }));

            await responseStream.WriteAsync(reply);
        }, context.CancellationToken);

        await context.CancellationToken;
    }

    public override Task<Empty> QueueJob(QueueJobRequest request, ServerCallContext context)
    {
        throw new NotImplementedException();
    }

    public override async Task ListJobs(FilteredJobsRequest request, IServerStreamWriter<JobReply> responseStream,
        ServerCallContext context)
    {
        var jobs = jobRepository.GetFilteredJobs(request.ToFilter());

        await jobs
            .Select(job => job.ToReply())
            .ForEachAwaitAsync(async r => await responseStream.WriteAsync(r));
    }

    public override Task<JobDetailsReply> GetJobDetails(JobDetailsRequest request, ServerCallContext context)
    {
        return base.GetJobDetails(request, context);
    }

    public override async Task ListenForJobDetailUpdates(JobDetailsRequest request,
        IServerStreamWriter<JobDetailsReply> responseStream, ServerCallContext context)
    {
        async Task Listener(Job job, JobUpdate update)
        {
            await responseStream.WriteAsync(job.ToDetailsReply());
        }

        var job = await jobRepository.GetByIdAsync(Guid.Parse(request.JobId));

        if (job == null)
        {
            throw new RpcException(new Status(StatusCode.NotFound, $"No job found for id {request.JobId}"), new Metadata
            {
                { "JobId", request.JobId }
            });
        }

        await Listener(job, JobUpdate.JobCreated);

        jobHub.RegisterListener(Listener, context.CancellationToken);

        await context.CancellationToken;
    }
}