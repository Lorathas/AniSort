using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AniSort.Core.Data;

public class ScheduledJob
{
    [Key]
    public Guid Id { get; set; }
    
    public string Name { get; set; }
    
    public JobType Type { get; set; }

    public ScheduleType ScheduleType { get; set; }

    public Struct ScheduleOptions { get; set; }

    public Struct Options { get; set; }
    
    public virtual ICollection<Job> Jobs { get; set; }
    
    public bool Deleted { get; set; }
}

public class ScheduledJobEntityTypeConfiguration : IEntityTypeConfiguration<ScheduledJob>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<ScheduledJob> builder)
    {
        builder.Property(j => j.ScheduleOptions)
            .HasConversion(
                p => p.ToByteArray(),
                p => Struct.Parser.ParseFrom(p));
        builder.Property(j => j.Options)
            .HasConversion(
                p => p.ToByteArray(),
                p => Struct.Parser.ParseFrom(p));
    }
}
