using System.Collections.Concurrent;
using AniSort.Core.Data;

namespace AniSort.Server.Jobs;

public class JobManager : IJobManager
{
    private readonly ILogger<JobManager> logger;
    private readonly ConcurrentDictionary<Guid, Func<Job, JobUpdate, Task>> listeners = new();

    private readonly ConcurrentDictionary<Guid, ConcurrentDictionary<Guid, Func<Job, JobUpdate, Task>>> jobListeners =
        new();

    private readonly ConcurrentDictionary<Guid, Guid> listenerJobMapping = new();

    public JobManager(ILogger<JobManager> logger)
    {
        this.logger = logger;
    }

    /// <inheritdoc/>
    public void PublishJobUpdate(Job job, JobUpdate update)
    {
        logger.LogTrace("Publishing Job {JobId} Update: {UpdateType}", job.Id, update);
        foreach (var listener in listeners.Values)
        {
            listener.Invoke(job, update);
        }
    }

    /// <inheritdoc/>
    public Guid RegisterUpdateListener(Func<Job, JobUpdate, Task> listener)
    {
        var id = Guid.NewGuid();
        listeners[id] = listener;
        return id;
    }

    /// <inheritdoc/>
    public void UnregisterUpdateListener(Guid listenerId)
    {
        listeners.Remove(listenerId, out _);
    }

    /// <inheritdoc/>
    public Guid RegisterJobUpdateListener(Guid jobId, Func<Job, JobUpdate, Task> listener)
    {
        var listenerDict =
            jobListeners.GetOrAdd(jobId, new ConcurrentDictionary<Guid, Func<Job, JobUpdate, Task>>());

        var id = Guid.NewGuid();
        listenerDict.TryAdd(id, listener);
        listenerJobMapping.TryAdd(id, jobId);
        return id;
    }

    /// <inheritdoc/>
    public void UnregisterJobUpdateListener(Guid listenerId)
    {
        if (listenerJobMapping.TryGetValue(listenerId, out var jobId))
        {
            listenerJobMapping.TryRemove(listenerId, out _);
            if (jobListeners.TryGetValue(jobId, out var listenerDict))
            {
                listenerDict.TryRemove(listenerId, out _);
            }
        }
    }
}