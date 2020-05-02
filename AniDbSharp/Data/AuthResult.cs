using System;
using System.Collections.Generic;
using System.Text;

namespace AniDbSharp.Data
{
    public class AuthResult
    {
        public bool Success { get; }
        public bool HasNewVersion { get; }

        public AuthResult(bool success, bool hasNewVersion = false)
        {
            Success = success;
            HasNewVersion = hasNewVersion;
        }
    }
}
