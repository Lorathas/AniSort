using System;
using System.Collections.Generic;
using System.Text;

namespace AniDbSharp
{
    public class AniDbResponse
    {
        public CommandStatus Status { get; }
        public string Message { get; }
        public string[] Body { get; }

        public AniDbResponse(CommandStatus status, string message)
        {
            Status = status;
            Message = message;
        }

        public AniDbResponse(CommandStatus status, string message, string[] body)
        {
            Status = status;
            Message = message;
            Body = body;
        }
    }
}
