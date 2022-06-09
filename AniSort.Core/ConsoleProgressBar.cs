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

using System;
using System.Text;
using AniSort.Core.Extensions;

namespace AniSort.Core
{
    /// <summary>
    /// Class for writing a progress bar in to console
    /// </summary>
    public class ConsoleProgressBar
    {
        private int spinnerPosition = 0;
        private readonly char[] spinnerCharacters = {'-', '\\', '|', '/'};
        public long Progress { get; set; }
        public long TotalProgress { get; }
        public int ProgressTicks { get; }
        public bool DrawSpinner { get; }
        public string PostfixMessage { get; }
        public string PostfixMessageShort { get;}
        public bool WritePercentage { get; }

        public ConsoleProgressBar(long totalProgress, int progressTicks, bool drawSpinner = true, bool writePercentage = true, string postfixMessage = null, string postfixMessageShort = null)
        {
            TotalProgress = totalProgress;
            ProgressTicks = progressTicks;
            DrawSpinner = drawSpinner;
            PostfixMessage = postfixMessage;
            PostfixMessageShort = postfixMessageShort;
            WritePercentage = writePercentage;
        }

        /// <summary>
        /// Write progress bar update to console
        /// </summary>
        public void WriteNextFrame()
        {
            double tickSize = (double) TotalProgress / ProgressTicks;

            int filledTicks = (int) Math.Floor(Progress / tickSize);

            var ticksBuilder = new StringBuilder(ProgressTicks);

            for (int idx = 0; idx < filledTicks; idx++)
            {
                ticksBuilder.Append('\u2588');
            }

            for (int idx = 0; idx < ProgressTicks - filledTicks; idx++)
            {
                ticksBuilder.Append('\u2591');
            }

            var lineBuilder = new StringBuilder();
            lineBuilder.Append("\r[");
            lineBuilder.Append(ticksBuilder);
            lineBuilder.Append("] ");

            if (DrawSpinner)
            {
                lineBuilder.Append(spinnerCharacters[spinnerPosition]);
            }


            if (WritePercentage)
            {
                double percentage = Math.Round(((double) Progress / TotalProgress) * 10000) / 100;

                lineBuilder.Append($" {percentage:F2}%");
            }

            int consoleWidth = Console.WindowWidth;

            if (!string.IsNullOrWhiteSpace(PostfixMessage))
            {
                lineBuilder.Append(' ');
                int builderSize = lineBuilder.Length;

                if (PostfixMessage.Length + builderSize > consoleWidth && !string.IsNullOrWhiteSpace(PostfixMessageShort))
                {
                    lineBuilder.Append(PostfixMessageShort);
                }
                else
                {
                    lineBuilder.Append(PostfixMessage);
                }
            }
            
            Console.Write(lineBuilder.ToString().Truncate(Console.WindowWidth));

            spinnerPosition = (spinnerPosition + 1) % spinnerCharacters.Length;
        }
    }
}
