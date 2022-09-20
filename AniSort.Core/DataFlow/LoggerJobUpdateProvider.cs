using System;
using System.Threading.Tasks;
using AniSort.Core.Data;
using Microsoft.Extensions.Logging;

namespace AniSort.Core.DataFlow;

public class LoggerJobUpdateProvider : IJobUpdateProvider
{
    private readonly ILogger<LoggerJobUpdateProvider> logger;

    public LoggerJobUpdateProvider(ILogger<LoggerJobUpdateProvider> logger)
    {
        this.logger = logger;
    }

    /// <inheritdoc />
    public void UpdateJobStatus(Job job)
    {
        switch (job.Status)
        {
            case JobStatus.Created:
                logger.LogInformation($"Job {job.Id} \"{job.Name}\" has been created");
                break;
            case JobStatus.Queued:
                logger.LogInformation($"Job {job.Id} \"{job.Name}\" has been queued for execution");
                break;
            case JobStatus.Running:
                logger.LogDebug($"Job {job.Id} \"{job.Name}\" running and is {Math.Round(job.PercentComplete, 2)}% complete");
                break;
            case JobStatus.Completed:
                logger.LogInformation($"Job {job.Id} \"{job.Name}\" has finished");
                break;
            case JobStatus.Failed:
                logger.LogError($"Job {job.Id} \"{job.Name}\" has failed");
                break;
        }
    }

    /// <inheritdoc />
    public Task UpdateJobStatusAsync(Job job)
    {
        switch (job.Status)
        {
            case JobStatus.Created:
                logger.LogInformation($"Job {job.Id} \"{job.Name}\" has been created");
                break;
            case JobStatus.Queued:
                logger.LogInformation($"Job {job.Id} \"{job.Name}\" has been queued for execution");
                break;
            case JobStatus.Running:
                logger.LogDebug($"Job {job.Id} \"{job.Name}\" running and is {Math.Round(job.PercentComplete, 2)}% complete");
                break;
            case JobStatus.Completed:
                logger.LogInformation($"Job {job.Id} \"{job.Name}\" has finished");
                break;
            case JobStatus.Failed:
                logger.LogError($"Job {job.Id} \"{job.Name}\" has failed");
                break;
        }
        return Task.CompletedTask;
    }
}
