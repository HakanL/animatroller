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

        [XmlElement("Controller")]
        public ControllerNode[] Controller { get; set; }
    }

    [Serializable()]
    [XmlType(AnonymousType = true)]
    public partial class ControllerNode
    {
        [XmlAttribute("Id")]
        public int Id { get; set; }

        [XmlAttribute("Name")]
        public string Name { get; set; }

        [XmlAttribute("Description")]
        public string Description { get; set; }

        [XmlAttribute("Type")]
        public string Type { get; set; }

        [XmlAttribute("Vendor")]
        public string Vendor { get; set; }

        [XmlAttribute("Model")]
        public string Model { get; set; }

        [XmlAttribute("Variant")]
        public string Variant { get; set; }

        [XmlAttribute("ActiveState")]
        public string ActiveState { get; set; }

        [XmlAttribute("AutoLayout")]
        public ushort AutoLayout { get; set; }

        [XmlAttribute("AutoUpload")]
        public ushort AutoUpload { get; set; }

        [XmlAttribute("SuppressDuplicates")]
        public ushort SuppressDuplicates { get; set; }

        [XmlAttribute("IP")]
        public string IP { get; set; }

        [XmlAttribute("Protocol")]
        public string Protocol { get; set; }

        [XmlAttribute("FPPProxy")]
        public string FPPProxy { get; set; }

        [XmlAttribute("Priority")]
        public ushort Priority { get; set; }

        [XmlElement("network")]
        public NetworkNode[] Network { get; set; }
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
