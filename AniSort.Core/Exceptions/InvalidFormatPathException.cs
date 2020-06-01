using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace AniSort.Core.Exceptions
{
    /// <summary>
    /// Exception for invalid path formats
    /// </summary>
    public class InvalidFormatPathException : Exception
    {
        /// <inheritdoc />
        public InvalidFormatPathException()
        {
        }

        /// <inheritdoc />
        protected InvalidFormatPathException(SerializationInfo? info, StreamingContext context) : base(info, context)
        {
        }

        /// <inheritdoc />
        public InvalidFormatPathException(string? message) : base(message)
        {
        }

        /// <inheritdoc />
        public InvalidFormatPathException(string? message, Exception? innerException) : base(message, innerException)
        {
        }
    }
}
