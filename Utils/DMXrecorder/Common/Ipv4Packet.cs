using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Net;

namespace Animatroller.Common
{
    public class Ipv4Packet
    {
        public byte[] DestinationMac { get; }

        public byte[] SourceMac { get; }

        public UInt16 IpType { get; }

        public UInt16 TotalLength { get; }

        public UInt16 Id { get; }

        public UInt16 FlagsFragmentOffset { get; }

        public byte TimeToLive { get; }

        public byte Protocol { get; }

        public UInt16 Checksum { get; }

        public IPAddress SourceAddress { get; }

        public IPAddress DestinationAddress { get; }

        protected BinaryReader binRead;

        public Ipv4Packet(Stream input)
        {
            this.binRead = new BinaryReader(input);

            DestinationMac = this.binRead.ReadBytes(6);
            SourceMac = this.binRead.ReadBytes(6);
            IpType = this.binRead.ReadUInt16();
            if (IpType != 0x08)
                throw new InvalidDataException($"Unknown ip type {IpType}");
            byte versionLength = this.binRead.ReadByte();
            if ((versionLength >> 4) != 4)
                throw new InvalidDataException($"Invalid version {versionLength >> 4}");
            byte differentiatedServicesField = this.binRead.ReadByte();
            TotalLength = ReadNetworkUInt16();
            Id = ReadNetworkUInt16();
            FlagsFragmentOffset = ReadNetworkUInt16();
            TimeToLive = this.binRead.ReadByte();
            Protocol = this.binRead.ReadByte();
            Checksum = ReadNetworkUInt16();
            SourceAddress = new IPAddress(this.binRead.ReadUInt32());
            DestinationAddress = new IPAddress(this.binRead.ReadUInt32());
            for (int i = 5; i < (versionLength & 0x0F); i++)
            {
                // Options
                this.binRead.ReadUInt32();
            }
        }

        protected UInt16 ReadNetworkUInt16()
        {
            return (UInt16)(this.binRead.ReadByte() << 8 | this.binRead.ReadByte());
        }
    }
}
