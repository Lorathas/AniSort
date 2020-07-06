using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;

namespace AniSort.Models
{
    [XmlRoot("animetitles")]
    public class AnimeTitles
    {
        [XmlArray(IsNullable = false), XmlArrayItem("anime")]
        public List<AnimeTitle> Titles { get; set; } = new List<AnimeTitle>();
    }

    public class AnimeTitle
    {
        [XmlAttribute()]
        public int AnimeId { get; set; }
    }
}
