using AniSort.Core.Data;
using AniSort.Server.JobManager;

namespace AniSort.Server.Jobs;

public interface IJobManager
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="job"></param>
    /// <param name="update"></param>
    void PublishJobUpdate(Job job, JobUpdate update);
    
    /// <summary>
    /// Register a listener for job updates
    /// </summary>
    /// <param name="listener">Listener to register</param>
    /// <returns>ID of the new listener for use with unregistering</returns>
    Guid RegisterUpdateListener(Func<Job, JobUpdate, Task> listener);
    
    /// <summary>
    /// Remove the listener for the ID, if it exists
    /// </summary>
    /// <param name="listenerId">ID of the listener</param>
    void UnregisterUpdateListener(Guid listenerId);

    /// <summary>
    /// Register an update listener for a specific job
    /// </summary>
    /// <param name="jobId">Id of the job to listen for updates for</param>
    /// <param name="listener">Listener to register</param>
    /// <returns></returns>
    Guid RegisterJobUpdateListener(Guid jobId, Func<Job, JobUpdate, Task> listener);
    
    /// <summary>
    /// Unregister a listener for a specific job
    /// </summary>
    /// <param name="listenerId"></param>
    void UnregisterJobUpdateListener(Guid listenerId);
}