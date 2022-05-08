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
using System.Xml.Serialization;

namespace AniSort.Core
{
    [XmlRoot("Config", IsNullable = false)]
    public class Config
    {
        public Mode Mode { get; set; } = Mode.Normal;

        public bool Debug { get; set; }

        public bool Verbose { get; set; }

        public bool Copy { get; set; }

        [XmlElement(IsNullable = false)]
        public AniDbConfig AniDb { get; set; } = new AniDbConfig();

        [XmlArray(IsNullable = false), XmlArrayItem("Source")]
        public List<string> Sources { get; set; } = new List<string>();

        [XmlElement(IsNullable = false)]
        public DestinationConfig Destination { get; set; } = new DestinationConfig();

        public bool IsValid => (Mode == Mode.Normal && !string.IsNullOrWhiteSpace(AniDb?.Username) &&
                                !string.IsNullOrWhiteSpace(AniDb?.Password)) ||
                               (Mode == Mode.Hash && Sources.Count > 0);
    }

    public class AniDbConfig
    {
        [XmlElement(IsNullable = false)]
        public string Username { get; set; }

        [XmlElement(IsNullable = false)]
        public string Password { get; set; }
    }

    public class DestinationConfig
    {
        [XmlElement(IsNullable = false)]
        public List<string> Paths { get; set; }
        
        [XmlElement(IsNullable = false)]
        public string NewFilePath { get; set; }

        [XmlElement]
        public bool FragmentSeries { get; set; } = true;

        [XmlElement(IsNullable = false)]
        public string Format { get; set; }

        [XmlElement(IsNullable = false)]
        public string TvPath { get; set; }

        [XmlElement(IsNullable = false)]
        public string MoviePath { get; set; }
    }
}