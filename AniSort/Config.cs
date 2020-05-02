using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;

namespace AniSort
{
    [XmlRoot("config", IsNullable = false)]
    class Config
    {
        public Mode Mode { get; set; } = Mode.Normal;

        public bool Debug { get; set; }

        public bool Verbose { get; set; }

        [XmlElement(IsNullable = false)]
        public AniDbConfig AniDb { get; set; }

        [XmlElement(IsNullable = false)]
        public string[] Sources { get; set; }
        
        [XmlElement(IsNullable = false)]
        public DestinationConfig Destination { get; set; }
    }

    class AniDbConfig
    {
        [XmlElement(IsNullable = false)]
        public string Username { get; set; }

        [XmlElement(IsNullable = false)]
        public string Password { get; set; }
    }

    class DestinationConfig
    {
        [XmlElement(IsNullable = false)]
        public string Path { get; set; }

        [XmlElement(IsNullable = false)]
        public string Format { get; set; }
    }
}