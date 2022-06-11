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
using System.Linq;
using System.Reflection;
using AniDbSharp.Exceptions;
using AniDbSharp.Extensions;

namespace AniDbSharp.Data
{
    public class FileResult
    {
        public bool FileFound { get; }
        public FileInfo FileInfo { get; }
        public FileAnimeInfo AnimeInfo { get; }

        public AniDbResponse Response { get; }

        public FileResult(bool fileFound, AniDbResponse response)
        {
            FileFound = fileFound;
            Response = response;
            FileInfo = new FileInfo();
            AnimeInfo = new FileAnimeInfo();
        }

        public void Parse(string rawValues, FileMask fileMask, FileAnimeMask animeMask)
        {
            string[] values = rawValues.Split('|');

            if (int.TryParse(values[0], out int fileId))
            {
                FileInfo.FileId = fileId;
            }
            else
            {
                throw new AniDbServerException($"Invalid value for FileId: {values[0]}");
            }

            int vdx = 1;

            ParseAndAssignValuesForEnum(FileInfo, fileMask.FirstByteFlags, values, ref vdx);
            ParseAndAssignValuesForEnum(FileInfo, fileMask.SecondByteFlags, values, ref vdx);
            ParseAndAssignValuesForEnum(FileInfo, fileMask.ThirdByteFlags, values, ref vdx);
            ParseAndAssignValuesForEnum(FileInfo, fileMask.FourthByteFlags, values, ref vdx);
            ParseAndAssignValuesForEnum(FileInfo, fileMask.FifthByteFlags, values, ref vdx);

            ParseAndAssignValuesForEnum(AnimeInfo, animeMask.FirstByteFlags, values, ref vdx);
            ParseAndAssignValuesForEnum(AnimeInfo, animeMask.SecondByteFlags, values, ref vdx);
            ParseAndAssignValuesForEnum(AnimeInfo, animeMask.ThirdByteFlags, values, ref vdx);
            ParseAndAssignValuesForEnum(AnimeInfo, animeMask.FourthByteFlags, values, ref vdx);
        }

        private static readonly string[] HashProperties = {"Ed2k", "Crc32", "Sha1", "Md5"};

        private static void ParseAndAssignValuesForEnum<TEnum>(object target, TEnum flags, string[] values, ref int valuesIndex) where TEnum : Enum
        {
            var targetType = target.GetType();

            foreach (var enumVal in Enum.GetValues(typeof(TEnum)).Cast<TEnum>().OrderByDescending(e => e))
            {
                if (flags.HasFlag(enumVal))
                {
                    string propertyName = Enum.GetName(typeof(TEnum), enumVal);

                    if (string.IsNullOrWhiteSpace(propertyName))
                    {
                        throw new AniDbException($"Unknown enum value {enumVal}");
                    }

                    if (HashProperties.Contains(propertyName))
                    {
                        propertyName += "Hash";
                    }

                    var property = targetType.GetProperty(propertyName);

                    TryParseForTypeAndSet(property, target, values[valuesIndex]);

                    valuesIndex++;
                }
            }
        }

        private static bool TryParseForTypeAndSet(PropertyInfo property, object target, string rawValue)
        {
            var byteArrayType = typeof(byte[]);
            var intType = typeof(int?);
            var shortType = typeof(short?);
            var longType = typeof(long?);
            var stringType = typeof(string);
            var fileStateType = typeof(FileState?);

            if (property.PropertyType == byteArrayType)
            {
                if (StringExtensions.TryParseHexString(rawValue, out byte[] bytes))
                {
                    property.SetValue(target, bytes);
                    return true;
                }

                return false;
            }
            else if (property.PropertyType == intType)
            {
                if (int.TryParse(rawValue, out int val))
                {
                    property.SetValue(target, val);
                    return true;
                }

                return false;
            }
            else if (property.PropertyType == shortType)
            {
                if (short.TryParse(rawValue, out short val))
                {
                    property.SetValue(target, val);
                    return true;
                }

                return false;
            }
            else if (property.PropertyType == longType)
            {
                if (long.TryParse(rawValue, out long val))
                {
                    property.SetValue(target, val);
                    return true;
                }

                return false;
            }
            else if (property.PropertyType == stringType)
            {
                property.SetValue(target, rawValue.Replace('`', '\'').Replace('/', '|').Replace("<br/>", "\n"));
                return true;
            }
            else if (property.PropertyType == fileStateType)
            {
                if (byte.TryParse(rawValue, out byte val))
                {
                    property.SetValue(target, (FileState) val);
                    return true;
                }

                return false;
            }
            else
            {
                throw new Exception($"Unsupported property type: {property.PropertyType}");
            }
        }
    }
}
