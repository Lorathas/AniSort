using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AniSort.Core.Data;

public class Job
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