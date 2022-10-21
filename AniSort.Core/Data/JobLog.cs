using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text.Json;
using AniSort.Core.Extensions;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AniSort.Core.Data;

public class JobLog : IEntity
{
    [Key]
    public Guid Id { get; set; }
    public Guid JobId { get; set; }
    public virtual Job Job { get; set; }
    public string Message { get; set; }
    public Struct Params { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public JobLog()
    {
    }

    public JobLog(Exception exception)
    {
        Message = exception.Message;
        Params ??= new Struct();
        Params.Fields["exception"].StringValue = exception.Message;
        Params.Fields["stackTrace"].StringValue = exception.StackTrace;
        Params.Fields["parameters"].ListValue = new ListValue();
    }

    public JobLog(Exception exception, string message, params object[] parameters)
    {
        Message = message;
        Params ??= new Struct();
        Params.Fields["exception"].StringValue = exception.Message;
        Params.Fields["stackTrace"].StringValue = exception.StackTrace;
        Params.Fields["parameters"].ListValue = parameters.ToListValue();
    }

    public JobLog(string message, params object[] parameters)
    {
        Message = message;
        Params ??= new Struct();
        Params.Fields["parameters"].ListValue = parameters.ToListValue();
    }

    /// <inheritdoc />
    public bool IsNew => Id != Guid.Empty;
}

public class JobLogEntityTypeConfiguration : IEntityTypeConfiguration<JobLog>
{
    public void Configure(EntityTypeBuilder<JobLog> builder)
    {
        builder.Property(l => l.Params)
            .HasConversion(
                p => p.ToByteArray(),
                p => Struct.Parser.ParseFrom(p));
    }
}