using System;
using System.Runtime.Serialization;

namespace AniDbSharp.Exceptions
{
    public class AniDbConnectionRefusedException : AniDbConnectionException
    {
        /// <inheritdoc />
        public AniDbConnectionRefusedException()
        {
        }

        /// <inheritdoc />
        protected AniDbConnectionRefusedException(SerializationInfo? info, StreamingContext context) : base(info, context)
        {
        }

        /// <inheritdoc />
        public AniDbConnectionRefusedException(string? message) : base(message)
        {
        }

        /// <inheritdoc />
        public AniDbConnectionRefusedException(string? message, Exception? innerException) : base(message, innerException)
        {
        }
    }
}