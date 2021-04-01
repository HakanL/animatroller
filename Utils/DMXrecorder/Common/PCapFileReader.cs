using Haukcode.PcapngUtils;
using System;
using System.IO;

namespace Animatroller.Common
{
    public abstract class PCapFileReader : IDisposable
    {
        private int framesRead = 0;
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

        public int FramesRead => this.framesRead;

        public void Rewind()
        {
            this.reader.Rewind();
        }

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

            this.framesRead++;

            using (var dataStream = new MemoryStream(pcapData.Data))
            {
                var packet = UdpPacket.CreateFromStream(dataStream);

                byte[] dataBytes = new byte[dataStream.Length - dataStream.Position];
                dataStream.Read(dataBytes, 0, dataBytes.Length);

                return (packet, dataBytes, pcapData.Seconds, pcapData.Microseconds);
            }
        }
    }
}
