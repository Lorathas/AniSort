using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using AniSort.Core.Commands;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AniSort.Core.Data;

public class Job : IEntity
{
    [Key] public Guid Id { get; set; }
    public string Name { get; set; }
    public JobType Type { get; set; }
    public JobStatus Status { get; set; }
    public virtual ICollection<JobStep> Steps { get; set; } = new List<JobStep>();
    public double PercentComplete => Steps.Count(s => s.IsFinished) / (double)Steps.Count;
    public bool IsFinished => Steps.Count(s => s.IsFinished) == Steps.Count;
    public DateTimeOffset QueuedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? StartedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public Struct Options { get; set; } = new();
    public Guid LocalFileId { get; set; }
    public virtual LocalFile LocalFile { get; set; }
    public virtual ICollection<JobLog> Logs { get; set; } = new List<JobLog>();
    public Guid? ScheduledJobId { get; set; }
    public virtual ScheduledJob ScheduledJob { get; set; }

    public string Path => Options.Fields[JobData.Path].StringValue;

    /// <inheritdoc />
    public bool IsNew => Id != Guid.Empty;

    public void Succeed()
    {
        Status = JobStatus.Completed;
        CompletedAt = DateTimeOffset.Now;
    }
    
    public void SucceedWith(string message, params object[] parameters)
    {
        Status = JobStatus.Completed;
        CompletedAt = DateTimeOffset.Now;
        Logs.Add(new JobLog(message, parameters));
    }

    public void FailWith(string message, params object[] parameters)
    {
        Status = JobStatus.Failed;
        CompletedAt = DateTimeOffset.Now;
        Logs.Add(new JobLog(message, parameters));
    }

    public void FailWith(Exception exception, string message, params object[] parameters)
    {
        Status = JobStatus.Failed;
        CompletedAt = DateTimeOffset.Now;
        Logs.Add(new JobLog(exception, message, parameters));
    }
}

public class JobEntityTypeConfiguration : IEntityTypeConfiguration<Job>
{
    public void Configure(EntityTypeBuilder<Job> builder)
    {
        builder.Property(e => e.Options)
            .HasConversion(
                o => o.ToByteArray(),
                o => Struct.Parser.ParseFrom(o));
    }
}