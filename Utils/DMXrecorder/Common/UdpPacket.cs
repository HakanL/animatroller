using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Net;

namespace Animatroller.Common
{
    public class UdpPacket : Ipv4Packet
    {
        public UInt16 SourcePort { get; set; }

        public UInt16 DestinationPort { get; set; }

        public UInt16 UdpLength => (UInt16)(this.payloadLength + 8);

        public UInt16 UdpChecksum { get; set; }

        private int payloadLength;

        public UdpPacket(int payloadLength = -1)
            : base(payloadLength + 8)
        {
            this.payloadLength = payloadLength;

            Protocol = 17;
        }

        public static UdpPacket CreateFromStream(Stream input)
        {
            var target = new UdpPacket(-1);
            target.ReadPacket(input);

            return target;
        }

        public override void ReadPacket(Stream input)
        {
            base.ReadPacket(input);

            if (Protocol != 17)
                throw new InvalidDataException($"Invalid protocol {Protocol}");

            var binReader = new BinaryReader(input);

            SourcePort = ReadNetworkUInt16(binReader);
            DestinationPort = ReadNetworkUInt16(binReader);
            int udpLength = ReadNetworkUInt16(binReader);
            this.payloadLength = udpLength - 8;
            UdpChecksum = ReadNetworkUInt16(binReader);
        }

        public void WritePacket(Stream stream, byte[] payload)
        {
            if (payload.Length != this.payloadLength)
                throw new ArgumentOutOfRangeException("Incorrect payload size");

            using (var ms = new MemoryStream(PacketLength))
            {
                ms.Position = base.HeaderSize;
                var binWriter = new BinaryWriter(ms);

                // Source port
                WriteNetwork(binWriter, SourcePort);
                // Destination port
                WriteNetwork(binWriter, DestinationPort);
                // UDP Length
                WriteNetwork(binWriter, UdpLength);
                // UDP Checksum
                int udpChecksumPos = (int)ms.Position;
                WriteNetwork(binWriter, 0);

                // Payload
                ms.Write(payload, 0, payload.Length);

                ms.Position = 0;
                base.WritePacket(ms);

                int udpStartPos = (int)ms.Position;

                var buf = ms.GetBuffer();

                int sum1 = CalculateChecksum(buf, base.HeaderSize - 8, 8, (17 << 16) + UdpLength);
                int sum2 = CalculateChecksum(buf, udpStartPos, (int)ms.Length - udpStartPos, sum1);
                UdpChecksum = WrapChecksum(sum2);

                buf[udpChecksumPos] = (byte)(UdpChecksum >> 8);
                buf[udpChecksumPos + 1] = (byte)(UdpChecksum & 0xFF);

                ms.Position = 0;
                ms.CopyTo(stream);
            }
        }
    }
}
