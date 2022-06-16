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
using System.Collections.Generic;
using System.Xml.Serialization;

namespace AniSort.Core
{
    [XmlRoot("Config", IsNullable = false)]
    public class Config
    {
        /// <summary>
        /// The mode/command to run when running
        /// </summary>
        public Mode Mode { get; set; } = Mode.Normal;

        /// <summary>
        /// Flag to enable debug mode and cause no changes to the file system
        /// </summary>
        public bool Debug { get; set; }

        /// <summary>
        /// Flag to enable verbose logging
        /// </summary>
        public bool Verbose { get; set; }

        /// <summary>
        /// Flag to copy files instead of moving them
        /// </summary>
        public bool Copy { get; set; }
        
        /// <summary>
        /// Perform incremental cleanup when processing files
        /// </summary>
        public bool IncrementalCleanup { get; set; }

        /// <summary>
        /// AniDB config
        /// </summary>
        [XmlElement(IsNullable = false)]
        public AniDbConfig AniDb { get; set; } = new();

        /// <summary>
        /// Sources to search for files in
        /// </summary>
        [XmlArray(IsNullable = false), XmlArrayItem("Source")]
        public List<string> Sources { get; set; } = new();
        
        /// <summary>
        /// List of library paths for the application
        /// </summary>
        [XmlArray(IsNullable = false), XmlArrayItem("Path")]
        public List<string> LibraryPaths { get; set; }

        [XmlElement]
        public bool IgnoreLibraryFiles { get; set; } = false;

        /// <summary>
        /// Config for file destinations
        /// </summary>
        [XmlElement(IsNullable = false)]
        public DestinationConfig Destination { get; set; } = new();
        
        [XmlIgnore]
        public bool IsValid => (Mode == Mode.Normal && !string.IsNullOrWhiteSpace(AniDb?.Username) &&
                                !string.IsNullOrWhiteSpace(AniDb?.Password)) ||
                               (Mode == Mode.Hash && Sources.Count > 0);
    }

    public class AniDbConfig
    {
        /// <summary>
        /// Username to login with
        /// </summary>
        [XmlElement(IsNullable = false)]
        public string Username { get; set; }

        /// <summary>
        /// Password to login with
        /// </summary>
        [XmlElement(IsNullable = false)]
        public string Password { get; set; }

        /// <summary>
        /// Max file search retries
        /// </summary>
        public int? MaxFileSearchRetries { get; set; } = 10;

        /// <summary>
        /// File search cooldown time in seconds
        /// </summary>
        public int FileSearchCooldownMinutes { get; set; } = 300;

        /// <summary>
        /// File search cooldown time
        /// </summary>
        [XmlIgnore]
        public TimeSpan FileSearchCooldown => TimeSpan.FromMinutes(FileSearchCooldownMinutes);
    }

    public class DestinationConfig
    {
        /// <summary>
        /// Path that new files will go to
        /// </summary>
        [XmlElement(IsNullable = false)]
        public string Path { get; set; }

        /// <summary>
        /// Flag to fragment series across paths (this only applies to single seasons)
        /// </summary>
        [XmlElement]
        public bool FragmentSeries { get; set; } = true;

        /// <summary>
        /// Format string for the files. See README.md for more in depth info on this
        /// </summary>
        [XmlElement(IsNullable = false)]
        public string Format { get; set; }

        /// <summary>
        /// Relative path to place tv shows
        /// </summary>
        [XmlElement(IsNullable = false)]
        public string TvPath { get; set; }

        /// <summary>
        /// Relative path to place movies and OVAs
        /// </summary>
        [XmlElement(IsNullable = false)]
        public string MoviePath { get; set; }
    }
}