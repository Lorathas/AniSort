using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace AniDbSharp.Exceptions
{
    public class AniDbServerException : AniDbException
    {
        /// <inheritdoc />
        public AniDbServerException()
        {
        }

        /// <inheritdoc />
        protected AniDbServerException(SerializationInfo? info, StreamingContext context) : base(info, context)
        {
        }

        /// <inheritdoc />
        public AniDbServerException(string? message) : base(message)
        {
        }

        /// <inheritdoc />
        public AniDbServerException(string? message, Exception? innerException) : base(message, innerException)
        {
        }
    }
}
