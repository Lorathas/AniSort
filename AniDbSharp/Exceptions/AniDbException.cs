using System;
using System.Runtime.Serialization;

namespace AniDbSharp.Exceptions
{
    public class AniDbException : Exception
    {
        /// <inheritdoc />
        public AniDbException()
        {
        }

        /// <inheritdoc />
        protected AniDbException(SerializationInfo? info, StreamingContext context) : base(info, context)
        {
        }

        /// <inheritdoc />
        public AniDbException(string? message) : base(message)
        {
        }

        /// <inheritdoc />
        public AniDbException(string? message, Exception? innerException) : base(message, innerException)
        {
        }
    }
}
