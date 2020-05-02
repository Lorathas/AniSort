// Copyright © 2020 Lorathas
//
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation
// files (the “Software”), to deal in the Software without restriction, including without limitation the rights to use, copy,
// modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the
// Software is furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED “AS IS”, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
// OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR
// IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using System.Collections.Generic;

namespace AniSort
{
    class Options
    {
        public Mode Mode { get; set; } = Mode.Normal;
        public bool DebugMode { get; set; }
        public bool VerboseLogging { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string ConfigFile { get; set; }
        public List<string> Sources { get; set; } = new List<string>();

        public bool IsValid
        {
            get
            {
                if (Mode == Mode.Normal && (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password)))
                {
                    return false;
                }

                if (Sources.Count == 0)
                {
                    return false;
                }

                return true;
            }
        }
    }
}
