using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace AniDbSharp.Exceptions
{
    public class AniDbConnectionException : AniDbException
    {
        /// <inheritdoc />
        public AniDbConnectionException()
        {
        }

        /// <inheritdoc />
        protected AniDbConnectionException(SerializationInfo? info, StreamingContext context) : base(info, context)
        {
        }

        /// <inheritdoc />
        public AniDbConnectionException(string? message) : base(message)
        {
        }

        /// <inheritdoc />
        public AniDbConnectionException(string? message, Exception? innerException) : base(message, innerException)
        {
        }
    }
}
