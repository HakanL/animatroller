using Haukcode.ArtNet.Packets;
using Haukcode.sACN.Model;
using System;
using System.Net;

namespace Animatroller.Common
{
    public class PCapArtNetFileWriter : PCapFileWriter, IFileWriter
    {
        public readonly byte[] BroadcastMac = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF };

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

        public static IPAddress GetUniverseAddress(int universe)
        {
            if (universe < 1 || universe > 63999)
                throw new InvalidOperationException("Unable to determine multicast group because the universe must be between 1 and 63999. Universes outside this range are not allowed.");

            byte[] group = new byte[] { 239, 255, 0, 0 };

            group[2] = (byte)((universe >> 8) & 0xff);     //Universe Hi Byte
            group[3] = (byte)(universe & 0xff);           //Universe Lo Byte

            return new IPAddress(group);
        }

        public static IPEndPoint GetUniverseEndPoint(int universe)
        {
            return new IPEndPoint(GetUniverseAddress(universe), 5568);
        }

        public void Output(DmxData dmxData)
        {
            if (dmxData.DataType == DmxData.DataTypes.NoChange)
                return;

            var packet = new ArtNetDmxPacket
            {
                DmxData = dmxData.Data,
                Universe = (short)(dmxData.Universe - 1),
                Sequence = (byte)dmxData.Sequence
            };

            var destinationEP = new IPEndPoint(IPAddress.Broadcast, 6454);

            WritePacket(BroadcastMac, destinationEP, packet.ToArray(), dmxData.TimestampMS);
        }

        public void Footer(int universeId)
        {
            // Ignore
        }
    }
}
