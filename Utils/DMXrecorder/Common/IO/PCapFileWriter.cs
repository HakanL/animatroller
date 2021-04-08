using Haukcode.PcapngUtils.Pcap;
using System;
using System.IO;
using System.Net;

namespace Animatroller.Common.IO
{
    public class PCapFileWriter : IDisposable
    {
        protected Haukcode.PcapngUtils.Common.IWriter writer;

        public PCapFileWriter(string fileName)
        {
            if (File.Exists(fileName))
                File.Delete(fileName);

            this.writer = new PcapWriter(fileName);
        }

        public void Dispose()
        {
            this.writer.Close();
            this.writer.Dispose();
        }

        public void WritePacket(byte[] destinationMac, IPEndPoint destinationEP, byte[] payload, double timestampMS)
        {
            uint secs = (uint)(timestampMS / 1000);
            uint usecs = (uint)(Math.Round(timestampMS * 1000, 3) - (secs * 1000000));

            WritePacket(destinationMac, destinationEP, payload, secs, usecs);
        }

        public void WritePacket(byte[] destinationMac, IPEndPoint destinationEP, byte[] payload, uint seconds, uint microSeconds)
        {
            var packet = new UdpPacket(payload.Length)
            {
                DestinationMac = destinationMac,
                SourceMac = new byte[6],            // Unknown/00:00:00:00:00:00
                TimeToLive = 0x3C,
                SourceAddress = new IPAddress(new byte[] { 0x01, 0x02, 0x03, 0x04 }),
                DestinationAddress = destinationEP.Address,
                SourcePort = (ushort)destinationEP.Port,
                DestinationPort = (ushort)destinationEP.Port,
            };

            using (var ms = new MemoryStream(packet.PacketLength))
            {
                packet.WritePacket(ms, payload);

                var pcapData = new PcapPacket(seconds, microSeconds, ms.ToArray(), 0);
                this.writer.WritePacket(pcapData);
            }
        }
    }
}
