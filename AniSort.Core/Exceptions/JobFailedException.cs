using System;
using System.Runtime.Serialization;

namespace AniSort.Core.Exceptions;

public class JobFailedException : JobException
{
    /// <inheritdoc />
    public JobFailedException()
    {
    }

    /// <inheritdoc />
    protected JobFailedException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }

    /// <inheritdoc />
    public JobFailedException(string message) : base(message)
    {
    }

    /// <inheritdoc />
    public JobFailedException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
