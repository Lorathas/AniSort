using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using AniSort.Core.Extensions;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AniSort.Core.Data;

public class StepLog : IEntity
{
    [Key] public Guid Id { get; set; }
    public Guid StepId { get; set; }
    public virtual JobStep Step { get; set; }
    public string Message { get; set; }
    public Struct Params { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public StepLog()
    {
    }

    public StepLog(Exception exception)
    {
        Message = exception.Message;
        Params ??= new Struct();
        Params.Fields["exception"].StringValue = exception.Message;
        Params.Fields["stackTrace"].StringValue = exception.StackTrace;
        Params.Fields["parameters"].ListValue = new ListValue();
    }

    public StepLog(Exception exception, string message, params object[] parameters)
    {
        Message = message;
        Params ??= new Struct();
        Params.Fields["exception"].StringValue = exception.Message;
        Params.Fields["stackTrace"].StringValue = exception.StackTrace;
        Params.Fields["parameters"].ListValue = parameters.ToListValue();
    }

    public StepLog(string message, params object[] parameters)
    {
        Message = message;
        Params ??= new Struct();
        Params.Fields["parameters"].ListValue = parameters.ToListValue();
    }

    /// <inheritdoc />
    public bool IsNew => Id != Guid.Empty;
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