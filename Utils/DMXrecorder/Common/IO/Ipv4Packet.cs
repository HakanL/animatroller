using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Net;

namespace Animatroller.Common.IO
{
    public class Ipv4Packet
    {
        public byte[] DestinationMac { get; set; } = new byte[6];

        public byte[] SourceMac { get; set; } = new byte[6];

        public UInt16 IpType { get; set; } = 0x0800;

        public byte Version { get; set; } = 0x04;

        public byte DifferentiatedServicesField { get; set; }

        public UInt16 TotalLength => (UInt16)(PacketLength - 14);

        public UInt16 Id { get; set; }

        public UInt16 FlagsFragmentOffset { get; set; }

        public byte TimeToLive { get; set; } = 0x3C;

        public byte Protocol { get; set; }

        public UInt16 Checksum { get; set; }

        public IPAddress SourceAddress { get; set; }

        public IPAddress DestinationAddress { get; set; }

        public IList<UInt32> Options { get; set; }

        private int payloadLength;

        public int PacketLength => this.payloadLength + HeaderSize;

        protected int HeaderSize => 14 + 20 + (Options?.Count ?? 0);

        public Ipv4Packet(int payloadLength = -1)
        {
            this.payloadLength = payloadLength;
        }

        public virtual void ReadPacket(Stream input)
        {
            var binReader = new BinaryReader(input);

            DestinationMac = binReader.ReadBytes(6);
            SourceMac = binReader.ReadBytes(6);
            IpType = ReadNetworkUInt16(binReader);
            if (IpType != 0x0800)
                throw new InvalidDataException($"Unknown ip type {IpType}");

            byte versionLength = binReader.ReadByte();
            Version = (byte)(versionLength >> 4);
            if (Version != 4)
                throw new InvalidDataException($"Invalid version {Version}");

            DifferentiatedServicesField = binReader.ReadByte();
            UInt16 totalLength = ReadNetworkUInt16(binReader);
            this.payloadLength = totalLength - 20;
            Id = ReadNetworkUInt16(binReader);
            FlagsFragmentOffset = ReadNetworkUInt16(binReader);
            TimeToLive = binReader.ReadByte();
            Protocol = binReader.ReadByte();
            Checksum = ReadNetworkUInt16(binReader);
            SourceAddress = new IPAddress(binReader.ReadUInt32());
            DestinationAddress = new IPAddress(binReader.ReadUInt32());

            var options = new List<UInt32>();
            for (int i = 5; i < (versionLength & 0x0F); i++)
            {
                // Options
                options.Add(binReader.ReadUInt32());
            }

            Options = options;
        }

        protected UInt16 ReadNetworkUInt16(BinaryReader binReader)
        {
            return (UInt16)(binReader.ReadByte() << 8 | binReader.ReadByte());
        }

        protected void WriteNetwork(BinaryWriter binWriter, UInt16 data)
        {
            binWriter.Write((byte)(data >> 8));
            binWriter.Write((byte)(data & 0xFF));
        }

        public void WritePacket(Stream stream)
        {
            using (var ms = new MemoryStream(HeaderSize))
            {
                var binWriter = new BinaryWriter(ms);

                // Destination Mac
                binWriter.Write(DestinationMac, 0, 6);
                // Source Mac
                binWriter.Write(SourceMac, 0, 6);
                // IP Type
                WriteNetwork(binWriter, IpType);
                // Version and header length
                byte versionLength = (byte)((Version << 4) + 0x05);
                if (Options != null)
                {
                    if (Options.Count > 10)
                        throw new ArgumentOutOfRangeException("Options size");
                    versionLength += (byte)Options.Count;
                }
                binWriter.Write(versionLength);
                // Differentiated Services Field
                binWriter.Write(DifferentiatedServicesField);
                // Total Length
                WriteNetwork(binWriter, TotalLength);
                // Id
                WriteNetwork(binWriter, Id);
                // Flags & Fragmentation
                WriteNetwork(binWriter, FlagsFragmentOffset);
                // TTL
                binWriter.Write(TimeToLive);
                // Protocol
                binWriter.Write(Protocol);
                // Checksum (calculated later)
                WriteNetwork(binWriter, 0);
                // Source Address
                binWriter.Write(SourceAddress.GetAddressBytes());
                // Destination Address
                binWriter.Write(DestinationAddress.GetAddressBytes());

                if (Options != null)
                {
                    foreach (UInt32 option in Options)
                        binWriter.Write(option);
                }

                // Calculate checksum
                var buf = ms.GetBuffer();

                Checksum = WrapChecksum(CalculateChecksum(buf, 14, (int)ms.Position - 14, 0));

                buf[24] = (byte)(Checksum >> 8);
                buf[25] = (byte)(Checksum & 0xFF);

                ms.Position = 0;
                ms.CopyTo(stream);
            }
        }

        protected ushort WrapChecksum(int sum)
        {
            return (ushort)~sum;
        }

        protected int CalculateChecksum(byte[] buf, int offset, int count, int sum)
        {
            int i;

            /* Checksum all the pairs of bytes first... */
            for (i = 0; i < (count & ~1U); i += 2)
            {
                sum += (ushort)(((buf[offset + i] << 8) & 0xFF00)
                    + (buf[offset + i + 1] & 0xFF));
                if (sum > 0xFFFF)
                    sum -= 0xFFFF;
            }

            /*
	         * If there's a single byte left over, checksum it, too.
	         * Network byte order is big-endian, so the remaining byte is
	         * the high byte.
	         */
            if (i < count)
            {
                sum += buf[offset + i] << 8;
                if (sum > 0xFFFF)
                    sum -= 0xFFFF;
            }

            return sum;
        }
    }
}
