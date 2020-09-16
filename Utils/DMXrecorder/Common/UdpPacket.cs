using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Net;

namespace Animatroller.Common
{
    public class UdpPacket : Ipv4Packet
    {
        public UInt16 SourcePort { get; }

        public UInt16 DestinationPort { get; }

        public UInt16 UdpLength { get; }

        public UInt16 UdpChecksum { get; }

        public UdpPacket(Stream input)
            : base(input)
        {
            if (Protocol != 17)
                throw new InvalidDataException($"Invalid protocol {Protocol}");

            SourcePort = ReadNetworkUInt16();
            DestinationPort = ReadNetworkUInt16();
            UdpLength = ReadNetworkUInt16();
            UdpChecksum = ReadNetworkUInt16();
        }
    }
}
