using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Animatroller.Common.IO.FileFormat
{
    [Serializable()]
    [XmlType(AnonymousType = true)]
    [XmlRoot(Namespace = "", IsNullable = false)]
    public partial class Networks
    {
        [XmlElement("network")]
        public NetworkNode[] Network { get; set; }

        [XmlAttribute("computer")]
        public string Computer { get; set; }
    }

    [Serializable()]
    [XmlType(AnonymousType = true)]
    public partial class NetworkNode
    {
        [XmlAttribute("ComPort")]
        public string ComPort { get; set; }

        [XmlAttribute("BaudRate")]
        public string BaudRate { get; set; }

        [XmlAttribute("NetworkType")]
        public string NetworkType { get; set; }

        [XmlAttribute("Description")]
        public string Description { get; set; }

        [XmlAttribute("MaxChannels")]
        public ushort MaxChannels { get; set; }
    }
}
