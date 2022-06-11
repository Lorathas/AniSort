// Copyright © 2022 Lorathas
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
using System.Text.RegularExpressions;

namespace AniDbSharp.Extensions
{
    public static class StringExtensions
    {
        private static readonly Regex HexVerify = new Regex("^[a-fA-F0-9]+$", RegexOptions.Compiled);

        /// <summary>
        /// Convert hex string to byte array
        /// </summary>
        /// <param name="hex">Hex string</param>
        /// <returns>Byte array of the hex data</returns>
        public static byte[] HexStringToBytes(this string hex)
        {
            if (hex.Length % 2 == 1)
            {
                throw new Exception($"{nameof(hex)} must be an even length.");
            }

            if (!HexVerify.IsMatch(hex))
            {
                throw new Exception("Invalid value detected.");
            }

            byte[] bytes = new byte[hex.Length >> 1];

            for (int idx = 0; idx < hex.Length >> 1; idx++)
            {
                bytes[idx] = (byte) ((GetHexVal(hex[idx << 1]) << 4) + GetHexVal(hex[(idx << 1) + 1]));
            }

            return bytes;
        }

        public static bool TryParseHexString(string hex, out byte[] bytes)
        {
            bytes = null;

            if (hex.Length % 2 == 1)
            {
                return false;
            }

            if (!HexVerify.IsMatch(hex))
            {
                return false;
            }

            bytes = new byte[hex.Length >> 1];

            for (int idx = 0; idx < hex.Length >> 1; idx++)
            {
                bytes[idx] = (byte)((GetHexVal(hex[idx << 1]) << 4) + GetHexVal(hex[(idx << 1) + 1]));
            }

            return true;
        }

        private static int GetHexVal(char hex)
        {
            int val = (int) hex;
            //For uppercase A-F letters:
            //return val - (val < 58 ? 48 : 55);
            //For lowercase a-f letters:
            //return val - (val < 58 ? 48 : 87);
            //Or the two combined, but a bit slower:
            return val - (val < 58 ? 48 : (val < 97 ? 55 : 87));
        }
    }
}