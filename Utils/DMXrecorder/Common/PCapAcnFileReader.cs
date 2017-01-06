using PcapngUtils;
using PcapngUtils.Pcap;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Animatroller.Common
{
    public class PCapAcnFileReader : IFileReader, IDisposable
    {
        private PcapngUtils.Common.IReader reader;
        private List<PcapngUtils.Common.IPacket> packets;
        private int readPosition;

        public bool DataAvailable
        {
            get { return this.packets.Count > this.readPosition; }
        }

        public PCapAcnFileReader(string fileName)
        {
            this.packets = new List<PcapngUtils.Common.IPacket>();
            this.reader = IReaderFactory.GetReader(fileName);
            this.reader.OnReadPacketEvent += Reader_OnReadPacketEvent;

            // Reads all packets
            this.reader.ReadPackets(CancellationToken.None);
        }

        private void Reader_OnReadPacketEvent(object context, PcapngUtils.Common.IPacket packet)
        {
            this.packets.Add(packet);
        }

        public void Dispose()
        {
            this.reader.Dispose();
        }

        private void ReadNetworkPacket(Stream input)
        {
            var binRead = new BinaryReader(input);

            var destinationMac = binRead.ReadBytes(6);
            var sourceMac = binRead.ReadBytes(6);
            UInt16 ipType = binRead.ReadUInt16();
            if (ipType != 0x08)
                throw new InvalidDataException("Unknown ip type " + ipType);
            byte versionLength = binRead.ReadByte();
            if ((versionLength >> 4) != 4)
                throw new InvalidDataException("Invalid version " + (versionLength >> 4));
            byte differentiatedServicesField = binRead.ReadByte();
            UInt16 totalLength = (UInt16)(binRead.ReadByte() << 8 | binRead.ReadByte());
            UInt16 id = (UInt16)(binRead.ReadByte() << 8 | binRead.ReadByte());
            UInt16 flagsFragmentOffset = (UInt16)(binRead.ReadByte() << 8 | binRead.ReadByte());
            byte timeToLive = binRead.ReadByte();
            byte protocol = binRead.ReadByte();
            if (protocol != 17)
                throw new InvalidDataException("Invalid protocol " + protocol);
            UInt16 checksum = (UInt16)(binRead.ReadByte() << 8 | binRead.ReadByte());
            var sourceAddress = new System.Net.IPAddress(binRead.ReadUInt32());
            var destinationAddress = new System.Net.IPAddress(binRead.ReadUInt32());
            for (int i = 5; i < (versionLength & 0x0F); i++)
            {
                // Options
                binRead.ReadUInt32();
            }

            // Start of UDP protocol
            UInt16 sourcePort = (UInt16)(binRead.ReadByte() << 8 | binRead.ReadByte());
            UInt16 destinationPort = (UInt16)(binRead.ReadByte() << 8 | binRead.ReadByte());
            UInt16 udpLength = (UInt16)(binRead.ReadByte() << 8 | binRead.ReadByte());
            UInt16 udpChecksum = (UInt16)(binRead.ReadByte() << 8 | binRead.ReadByte());
        }

        public DmxData ReadFrame()
        {
            if (!DataAvailable)
                throw new EndOfStreamException();

            var pcapData = this.packets[this.readPosition++];

            var dataStream = new MemoryStream(pcapData.Data);
            ReadNetworkPacket(dataStream);
            var reader = new Acn.IO.AcnBinaryReader(dataStream);
            var packet = Acn.AcnPacket.ReadPacket(reader) as Acn.Packets.sAcn.StreamingAcnDmxPacket;
            if (packet == null)
                throw new InvalidDataException("Not a valid Streaming DMX ACN packet");

            byte[] dmxData;
            if (packet.Dmx.StartCode == 0xff)
            {
                dmxData = new byte[packet.Dmx.Data.Length - 1];
                Buffer.BlockCopy(packet.Dmx.Data, 1, dmxData, 0, dmxData.Length);
            }
            else
                dmxData = packet.Dmx.Data;

            return new DmxData
            {
                DataType = DmxData.DataTypes.FullFrame,
                Sequence = packet.Framing.SequenceNumber,
                TimestampMS = pcapData.Seconds * 1000 + (ulong)(pcapData.Microseconds / 1000),
                Universe = packet.Framing.Universe,
                Data = dmxData
            };
        }

        public void Rewind()
        {
            this.readPosition = 0;
        }
    }
}
