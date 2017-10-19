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
    public class PCapAcnFileWriter : IFileWriter, IDisposable
    {
        public readonly Guid AcnId = new Guid("{29D35C91-702E-4B7E-9ACD-D343FD15DDEE}");
        public const string AcnSourceName = "DmxFileWriter";

        private PcapngUtils.Common.IWriter writer;
        private byte priority;

        public PCapAcnFileWriter(string fileName, byte priority = 100)
        {
            if (File.Exists(fileName))
                File.Delete(fileName);

            this.writer = new PcapWriter(fileName);
            this.priority = priority;
        }

        public void Dispose()
        {
            this.writer.Close();
            this.writer.Dispose();
        }

        private byte[] GetMacFromMulticastIP(System.Net.IPAddress input)
        {
            if (input.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork)
                throw new ArgumentException("Only supports IPV4 now");

            var addr = input.GetAddressBytes();

            var mac = new byte[6];
            mac[0] = 0x01;
            mac[1] = 0;
            mac[2] = 0x5E;
            mac[3] = (byte)(addr[1] & 0x7F);
            mac[4] = addr[2];
            mac[5] = addr[3];

            return mac;
        }

        private void BuildNetworkPacket(Stream stream, System.Net.IPEndPoint destinationEP, int packetLength)
        {
            var writer = new BinaryWriter(stream);

            writer.Write(GetMacFromMulticastIP(destinationEP.Address));
            // Source (unknown)
            writer.Write(new byte[6]);
            // IP Type
            writer.Write((byte)0x08);
            writer.Write((byte)0x00);
            // Version and header length
            writer.Write((byte)0x45);
            // Differentiated Services Field
            writer.Write((byte)0x00);
            // Total Length (666 bytes for 512 channels)
            int ipTotalSize = packetLength + 20 + 8;
            writer.Write((byte)(ipTotalSize >> 8));
            writer.Write((byte)(ipTotalSize & 0xFF));
            // Id
            writer.Write((byte)0x00);
            writer.Write((byte)0x00);
            // Flags & Fragmentation
            writer.Write((byte)0x00);
            writer.Write((byte)0x00);
            // TTL
            writer.Write((byte)0x3C);
            // Protocol (UDP)
            writer.Write((byte)17);
            // Checksum (calculated later)
            writer.Write((byte)0x00);
            writer.Write((byte)0x00);

            // Source Address
            writer.Write((byte)0x01);
            writer.Write((byte)0x02);
            writer.Write((byte)0x03);
            writer.Write((byte)0x04);
            // Destination Address
            writer.Write(destinationEP.Address.GetAddressBytes());

            // Start of UDP protocol
            // Source port (using destination since we don't have the source port)
            writer.Write((byte)(destinationEP.Port >> 8));
            writer.Write((byte)(destinationEP.Port & 0xFF));
            // Destination port
            writer.Write((byte)(destinationEP.Port >> 8));
            writer.Write((byte)(destinationEP.Port & 0xFF));
            // UDP Length (646 bytes for 512 channels)
            int udpPacketSize = packetLength + 8;
            writer.Write((byte)(udpPacketSize >> 8));
            writer.Write((byte)(udpPacketSize & 0xFF));
            // UDP Checksum
            writer.Write((byte)0x00);
            writer.Write((byte)0x00);
        }

        public void Header(int universeId)
        {
            // Ignore
        }

        private int CalculateChecksum(byte[] buf, int offset, int count, int sum)
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

        private ushort WrapChecksum(int sum)
        {
            return (ushort)~sum;
        }

        private void SetIPCheckSum(MemoryStream memStream)
        {
            var buf = memStream.GetBuffer();

            ushort sum = WrapChecksum(CalculateChecksum(buf, 14, 20, 0));

            buf[24] = (byte)(sum >> 8);
            buf[25] = (byte)(sum & 0xFF);
        }

        private void SetUDPCheckSum(MemoryStream memStream)
        {
            var buf = memStream.GetBuffer();

            int sum1 = CalculateChecksum(buf, 26, 8, (17 << 16) + ((int)buf[38] << 8 | buf[39]));
            int sum2 = CalculateChecksum(buf, 34, (int)memStream.Length - 34, sum1);
            ushort sum = WrapChecksum(sum2);

            buf[40] = (byte)(sum >> 8);
            buf[41] = (byte)(sum & 0xFF);
        }

        public void Output(DmxData dmxData)
        {
            var packet = new Acn.Packets.sAcn.StreamingAcnDmxPacket();
            packet.Framing.SourceName = AcnSourceName;
            packet.Framing.Universe = (short)dmxData.Universe;
            packet.Framing.Priority = this.priority;
            packet.Framing.SequenceNumber = (byte)dmxData.Sequence;
            packet.Dmx.StartCode = 0;
            packet.Dmx.Data = dmxData.Data;

            var destinationEP = Acn.Sockets.StreamingAcnSocket.GetUniverseEndPoint(dmxData.Universe);

            packet.Root.SenderId = AcnId;

            using (var data = new MemoryStream())
            {
                var writer = new Acn.IO.AcnBinaryWriter(data);

                Acn.AcnPacket.WritePacket(packet, writer);

                using (var networkData = new MemoryStream())
                {
                    BuildNetworkPacket(networkData, destinationEP, (int)data.Position);

                    SetIPCheckSum(networkData);

                    data.Position = 0;
                    data.WriteTo(networkData);

                    SetUDPCheckSum(networkData);

                    ulong secs = dmxData.TimestampMS / 1000;
                    ulong usecs = (dmxData.TimestampMS * 1000) - (secs * 1000000);
                    var pcapData = new PcapngUtils.Pcap.PcapPacket(secs, usecs, networkData.ToArray(), 0);
                    this.writer.WritePacket(pcapData);
                }
            }
        }

        public void Footer(int universeId)
        {
            // Ignore
        }
    }
}
