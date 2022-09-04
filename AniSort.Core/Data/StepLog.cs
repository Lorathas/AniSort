using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AniSort.Core.Data;

public class StepLog
{
    [Key] public Guid Id { get; set; }
    public Guid StepId { get; set; }
    public virtual JobStep Step { get; set; }
    public string Message { get; set; }
    public Struct Params { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public class StepLogEntityTypeConfiguration : IEntityTypeConfiguration<StepLog>
{
    public void Configure(EntityTypeBuilder<StepLog> builder)
    {
        builder.Property(l => l.Params)
            .HasConversion(
                p => p.ToByteArray(),
                p => Struct.Parser.ParseFrom(p));
    }
}