using System.Threading.Tasks.Dataflow;
using AniSort.Core.Data;
using AniSort.Core.Data.Repositories;
using Grpc.Core;
using AniSort.Server;
using AniSort.Server.Extensions;
using AniSort.Server.Jobs;
using Google.Protobuf.Collections;
using Google.Protobuf.WellKnownTypes;

namespace AniSort.Server.Services;

public class JobService : Server.JobService.JobServiceBase
{
    private readonly ILogger<JobService> logger;
    private readonly IJobManager jobManager;
    private readonly IJobRepository jobRepository;

    public JobService(ILogger<JobService> logger, IJobManager jobManager, IJobRepository jobRepository)
    {
        this.logger = logger;
        this.jobManager = jobManager;
        this.jobRepository = jobRepository;
    }

    public override async Task ListenForJobChanges(FilteredJobsRequest request,
        IServerStreamWriter<JobUpdateReply> responseStream, ServerCallContext context)
    {
        // TODO: Add filters to job manager and calls 

        var listenerId = jobManager.RegisterUpdateListener(async (job, update) =>
        {
            var reply = new JobUpdateReply
            {
                JobId = job.Id.ToString(),
                Name = job.Name,
                Type = (JobType)job.Type,
                Status = (JobStatus)job.Status,
                PercentComplete = job.PercentComplete,
                IsFinished = job.IsFinished,
                UpdateType = update,
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
        });

        await context.CancellationToken;

        jobManager.UnregisterUpdateListener(listenerId);
    }

    public override async Task<Empty> QueueJob(QueueJobRequest request, ServerCallContext context)
    {
        throw new NotImplementedException();
    }

    public override async Task ListJobs(FilteredJobsRequest request, IServerStreamWriter<JobReply> responseStream,
        ServerCallContext context)
    {
        var jobs = jobRepository.GetFilteredJobs(request.ToJobFilter());

        await jobs.Select(job =>
            {
                var reply = new JobReply
                {
                    JobId = job.Id.ToString(),
                    Name = job.Name,
                    Type = (JobType)job.Type,
                    Status = (JobStatus)job.Status,
                    PercentComplete = job.PercentComplete,
                    IsFinished = job.IsFinished,
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

                return reply;
            })
            .ForEachAwaitAsync(async r => await responseStream.WriteAsync(r));
    }

    public override async Task GetJobDetails(JobDetailsRequest request,
        IServerStreamWriter<JobDetailsReply> responseStream, ServerCallContext context)
    {
        async Task Listener(Job job, JobUpdate update)
        {
            var reply = new JobDetailsReply();

            reply.Logs.AddRange(job.Logs.Select(l => new JobLog
            {
                JobLogId = l.Id.ToString(),
                JobId = l.JobId.ToString(),
                Message = l.Message,
                Params = l.Params,
                CreatedAt = l.CreatedAt.ToTimestamp(),
            }));

            reply.Steps.AddRange(job.Steps.Select(s =>
            {
                var stepDetails = new JobDetailsReply.Types.JobStepDetails { Name = s.Name, StartedAt = s.StartedAt?.ToTimestamp(), CompletedAt = s.CompletedAt?.ToTimestamp() };

                stepDetails.Logs.AddRange(s.Logs.Select(l => new StepLog { StepLogId = l.Id.ToString(), Message = l.Message, Params = l.Params, CreatedAt = l.CreatedAt.ToTimestamp() }));

                return stepDetails;
            }));

            await responseStream.WriteAsync(reply);
        }

        var job = await jobRepository.GetByIdAsync(Guid.Parse(request.JobId));

        if (job == null)
        {
            return;
        }

        await Listener(job, JobUpdate.JobCreated);

        var id = jobManager.RegisterJobUpdateListener(job.Id, Listener);

        await context.CancellationToken;

        jobManager.UnregisterJobUpdateListener(id);
    }
}