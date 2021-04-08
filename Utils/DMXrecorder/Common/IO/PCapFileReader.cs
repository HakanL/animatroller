using Haukcode.PcapngUtils;
using System;
using System.IO;

namespace Animatroller.Common.IO
{
    public abstract class PCapFileReader : IDisposable
    {
        private readonly Haukcode.PcapngUtils.Common.IReader reader;

        public PCapFileReader(string fileName)
        {
            this.reader = IReaderFactory.GetReader(fileName);
        }

        public void Dispose()
        {
            this.reader.Dispose();
        }

        public bool DataAvailable => this.reader.MoreAvailable;

        protected (Ipv4Packet Packet, byte[] Payload, ulong Seconds, ulong Microseconds) ReadPacket()
        {
            if (!DataAvailable)
                throw new EndOfStreamException();

            var pcapData = this.reader.ReadNextPacket();

            if (pcapData == null)
            {
                // Packet that we ignore was read and then the stream ended
                return (null, null, 0, 0);
            }

            using var dataStream = new MemoryStream(pcapData.Data);
            var packet = UdpPacket.CreateFromStream(dataStream);

            byte[] dataBytes = new byte[dataStream.Length - dataStream.Position];
            dataStream.Read(dataBytes, 0, dataBytes.Length);

            return (packet, dataBytes, pcapData.Seconds, pcapData.Microseconds);
        }
    }
}
