using Haukcode.ArtNet.Packets;
using Haukcode.sACN.Model;
using System;
using System.Net;

namespace Animatroller.Common.IO
{
    public class PCapArtNetFileWriter : PCapFileWriter, IFileWriter
    {
        public readonly byte[] BroadcastMac = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF };
        public const ushort ArtNetPort = 6454;

        public PCapArtNetFileWriter(string fileName)
            : base(fileName)
        {
        }

        public static byte[] GetMacFromMulticastIP(IPAddress input)
        {
            if (input.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork)
                throw new ArgumentException("Only supports IPV4 for now");

            byte[] addr = input.GetAddressBytes();

            byte[] mac = new byte[6];
            mac[0] = 0x01;
            mac[1] = 0;
            mac[2] = 0x5E;
            mac[3] = (byte)(addr[1] & 0x7F);
            mac[4] = addr[2];
            mac[5] = addr[3];

            return mac;
        }

        public void Header(int universeId)
        {
            // Ignore
        }

        public void Output(DmxDataOutputPacket dmxData)
        {
            //FIXME: Handle ArtNet sync
            var dmxFrame = dmxData.Content as DmxDataFrame;

            if (dmxFrame != null)
            {
                var packet = new ArtNetDmxPacket
                {
                    DmxData = dmxFrame.Data,
                    Universe = (short)(dmxFrame.UniverseId - 1),
                    Sequence = (byte)dmxData.Sequence
                };

                IPEndPoint destinationEP;
                if (dmxFrame.Destination != null)
                    destinationEP = new IPEndPoint(dmxFrame.Destination, ArtNetPort);
                else
                    destinationEP = new IPEndPoint(IPAddress.Broadcast, ArtNetPort);

                WritePacket(BroadcastMac, destinationEP, packet.ToArray(), dmxData.TimestampMS);
            }
        }

        public void Footer(int universeId)
        {
            // Ignore
        }
    }
}
