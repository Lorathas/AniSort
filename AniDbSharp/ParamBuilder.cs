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
using System.Collections.Generic;
using System.Text;
using System.Web;

namespace AniDbSharp
{
    /// <summary>
    /// Builder for parameters for AniDB commands
    /// </summary>
    public class ParamBuilder
    {
        private readonly List<(string Param, string Value)> parameters = new List<(string Param, string Value)>();

        /// <inheritdoc />
        public override string ToString()
        {
            var builder = new StringBuilder();

            for (int idx = 0; idx < parameters.Count; idx++)
            {
                var (key, value) = parameters[idx];

                builder.Append(key);
                builder.Append('=');
                builder.Append(EncodeParameter(value));

                if (idx != parameters.Count - 1)
                {
                    builder.Append('&');
                }
            }

            return builder.ToString();
        }

        /// <summary>
        /// Add string param
        /// </summary>
        /// <param name="param">Parameter name</param>
        /// <param name="value">Value to send for parameter</param>
        public void Add(string param, string value)
        {
            parameters.Add((param, value));
        }

        /// <summary>
        /// Add int param
        /// </summary>
        /// <param name="param">Parameter name</param>
        /// <param name="value">Value to send for parameter</param>
        public void Add(string param, int value)
        {
            parameters.Add((param, value.ToString()));
        }

        /// <summary>
        /// Add long param
        /// </summary>
        /// <param name="param">Parameter name</param>
        /// <param name="value">Value to send for parameter</param>
        public void Add(string param, long value)
        {
            parameters.Add((param, value.ToString()));
        }

        /// <summary>
        /// Add short param
        /// </summary>
        /// <param name="param">Parameter name</param>
        /// <param name="value">Value to send for parameter</param>
        public void Add(string param, short value)
        {
            parameters.Add((param, value.ToString()));
        }

        /// <summary>
        /// Add bool param
        /// </summary>
        /// <remarks>
        /// Data will be sent over as 1 for true, 0 for false.
        /// </remarks>
        /// <param name="param">Parameter name</param>
        /// <param name="value">Boolean value to send</param>
        public void Add(string param, bool value)
        {
            parameters.Add((param, value ? "1" : "0"));
        }

        /// <summary>
        /// Add byte array param
        /// </summary>
        /// <remarks>
        /// Data will be sent over as a hex string
        /// </remarks>
        /// <param name="param">Parameter name</param>
        /// <param name="value">Bytes to add a hex string for</param>
        public void Add(string param, byte[] value)
        {
            parameters.Add((param, BitConverter.ToString(value).Replace("-", string.Empty)));
        }

        public static string EncodeParameter(string parameter)
        {
            return HttpUtility.HtmlEncode(parameter).Replace("\n", "<br/>").Replace("&#xD;&#xA;", "<br/>");
        }
    }
}