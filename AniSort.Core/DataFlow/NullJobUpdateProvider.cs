using System.Threading.Tasks;
using AniSort.Core.Data;

namespace AniSort.Core.DataFlow;

public class NullJobUpdateProvider : IJobUpdateProvider
{
    /// <inheritdoc />
    public void UpdateJobStatus(Job job)
    {
        // Do nothing
    }

    /// <inheritdoc />
    public Task UpdateJobStatusAsync(Job job)
    {
        // Do nothing
        return Task.CompletedTask;
    }
}
