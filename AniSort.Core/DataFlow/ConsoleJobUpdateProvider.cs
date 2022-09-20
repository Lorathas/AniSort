using System;
using System.Threading.Tasks;
using AniSort.Core.Data;

namespace AniSort.Core.DataFlow;

public class ConsoleJobUpdateProvider : IJobUpdateProvider
{
    /// <inheritdoc />
    public void UpdateJobStatus(Job job)
    {
        switch (job.Status)
        {
            case JobStatus.Created:
                Console.WriteLine($"Job {job.Id} \"{job.Name}\" has been created");
                break;
            case JobStatus.Queued:
                Console.WriteLine($"Job {job.Id} \"{job.Name}\" has been queued for execution");
                break;
            case JobStatus.Running:
                Console.WriteLine($"Job {job.Id} \"{job.Name}\" running and is {Math.Round(job.PercentComplete, 2)}% complete");
                break;
            case JobStatus.Completed:
                Console.WriteLine($"Job {job.Id} \"{job.Name}\" has finished");
                break;
            case JobStatus.Failed:
                Console.WriteLine($"Job {job.Id} \"{job.Name}\" has failed");
                break;
        }
    }

    /// <inheritdoc />
    public Task UpdateJobStatusAsync(Job job)
    {
        switch (job.Status)
        {
            case JobStatus.Created:
                Console.WriteLine($"Job {job.Id} \"{job.Name}\" has been created");
                break;
            case JobStatus.Queued:
                Console.WriteLine($"Job {job.Id} \"{job.Name}\" has been queued for execution");
                break;
            case JobStatus.Running:
                Console.WriteLine($"Job {job.Id} \"{job.Name}\" running and is {Math.Round(job.PercentComplete, 2)}% complete");
                break;
            case JobStatus.Completed:
                Console.WriteLine($"Job {job.Id} \"{job.Name}\" has finished");
                break;
            case JobStatus.Failed:
                Console.WriteLine($"Job {job.Id} \"{job.Name}\" has failed");
                break;
        }
        return Task.CompletedTask;
    }
}
