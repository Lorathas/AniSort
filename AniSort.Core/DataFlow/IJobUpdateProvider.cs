using System.Threading.Tasks;
using AniSort.Core.Data;

namespace AniSort.Core.DataFlow;

public interface IJobUpdateProvider
{
    /// <summary>
    /// Update the status of a job
    /// </summary>
    /// <param name="job">Current state of the job</param>
    void UpdateJobStatus(Job job);

    /// <summary>
    /// Update the status of a job asynchronously
    /// </summary>
    /// <param name="job">Current state of the job</param>
    Task UpdateJobStatusAsync(Job job);
}
