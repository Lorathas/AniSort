using System;
using System.Runtime.Serialization;

namespace AniSort.Core.Exceptions;

public class UnsupportedJobTypeException : JobException
{
    /// <inheritdoc />
    public UnsupportedJobTypeException()
    {
    }

    /// <inheritdoc />
    protected UnsupportedJobTypeException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }

    /// <inheritdoc />
    public UnsupportedJobTypeException(string message) : base(message)
    {
    }

    /// <inheritdoc />
    public UnsupportedJobTypeException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
