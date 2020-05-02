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