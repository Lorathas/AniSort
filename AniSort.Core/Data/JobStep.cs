using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace AniSort.Core.Data;

public class JobStep : IEntity
{
    [Key]
    public Guid Id { get; set; }

    public Guid JobId { get; set; }

    public virtual Job Job { get; set; }

    public string Name { get; set; }
    
    public StepType Type { get; set; }

    public JobStatus Status { get; set; }

    public bool IsFinished => Status is JobStatus.Completed or JobStatus.Failed;

    public double PercentComplete => CurrentProgress / (double) TotalProgress;

    public long CurrentProgress { get; set; }

    public long TotalProgress { get; set; }

    public DateTimeOffset? StartedAt { get; set; }

    public DateTimeOffset? CompletedAt { get; set; }

    public virtual ICollection<StepLog> Logs { get; set; } = new List<StepLog>();

    /// <inheritdoc />
    public bool IsNew => Id != Guid.Empty;

    public void Succeed()
    {
        Status = JobStatus.Completed;
        CompletedAt = DateTimeOffset.Now;
        CurrentProgress = TotalProgress;
    }
    
    public void SucceedWith(string message, params object[] parameters)
    {
        Status = JobStatus.Completed;
        CompletedAt = DateTimeOffset.Now;
        Logs.Add(new StepLog(message, parameters));
        CurrentProgress = TotalProgress;
    }
    
    public void FailWith(string message, params object[] parameters)
    {
        Status = JobStatus.Failed;
        CompletedAt = DateTimeOffset.Now;
        Logs.Add(new StepLog(message, parameters));
        CurrentProgress = TotalProgress;
    }

    public void FailWith(Exception exception, string message, params object[] parameters)
    {
        Status = JobStatus.Failed;
        CompletedAt = DateTimeOffset.Now;
        Logs.Add(new StepLog(exception, message, parameters));
        CurrentProgress = TotalProgress;
    }
}
