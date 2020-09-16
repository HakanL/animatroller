using Haukcode.PcapngUtils;
using Haukcode.PcapngUtils.Pcap;
using Haukcode.sACN;
using Haukcode.sACN.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Animatroller.Common
{
    public abstract class PCapFileReader : IDisposable
    {
        private readonly Haukcode.PcapngUtils.Common.IReader reader;
        private readonly List<Haukcode.PcapngUtils.Common.IPacket> packets;
        private int readPosition;

        public PCapFileReader(string fileName)
        {
            this.packets = new List<Haukcode.PcapngUtils.Common.IPacket>();
            this.reader = IReaderFactory.GetReader(fileName);
            this.reader.OnReadPacketEvent += Reader_OnReadPacketEvent;

            // Reads all packets
            this.reader.ReadPackets(CancellationToken.None);
        }

        private void Reader_OnReadPacketEvent(object context, Haukcode.PcapngUtils.Common.IPacket packet)
        {
            this.packets.Add(packet);
        }

        public void Dispose()
        {
            this.reader.Dispose();
        }

        public bool DataAvailable
        {
            get { return this.packets.Count > this.readPosition; }
        }

        public void Rewind()
        {
            this.readPosition = 0;
        }

        protected (Ipv4Packet Packet, byte[] Payload, ulong Seconds, ulong Microseconds) ReadPacket()
        {
            if (!DataAvailable)
                throw new EndOfStreamException();

            var pcapData = this.packets[this.readPosition++];

            var dataStream = new MemoryStream(pcapData.Data);

            var packet = UdpPacket.CreateFromStream(dataStream);

            byte[] dataBytes = new byte[dataStream.Length - dataStream.Position];
            dataStream.Read(dataBytes, 0, dataBytes.Length);

            return (packet, dataBytes, pcapData.Seconds, pcapData.Microseconds);
        }
    }
}
