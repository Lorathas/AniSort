using System;
using System.Runtime.Serialization;

namespace AniSort.Core.Exceptions;

public class JobException : ApplicationException
{
    /// <inheritdoc />
    public JobException()
    {
    }

    /// <inheritdoc />
    protected JobException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }

    /// <inheritdoc />
    public JobException(string message) : base(message)
    {
    }

    /// <inheritdoc />
    public JobException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
