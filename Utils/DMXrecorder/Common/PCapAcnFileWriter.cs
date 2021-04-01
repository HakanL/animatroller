using Haukcode.sACN.Model;
using System;
using System.Net;

namespace Animatroller.Common
{
    public class PCapAcnFileWriter : PCapFileWriter, IFileWriter
    {
        public readonly Guid AcnId = new Guid("{29D35C91-702E-4B7E-9ACD-D343FD15DDEE}");
        public const string AcnSourceName = "DmxFileWriter";

        private readonly byte priority;

        public PCapAcnFileWriter(string fileName, byte priority = 100)
            : base(fileName)
        {
            this.priority = priority;
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

        public void Output(DmxDataPacket dmxData)
        {
            SACNDataPacket packet;
            IPEndPoint destinationEP;

            switch (dmxData.DataType)
            {
                case DmxDataFrame.DataTypes.FullFrame:
                    packet = new SACNDataPacket(RootLayer.CreateRootLayerData(
                        uuid: AcnId,
                        sourceName: AcnSourceName,
                        universeID: (ushort)dmxData.UniverseId,
                        sequenceID: (byte)dmxData.Sequence,
                        data: dmxData.Data,
                        priority: this.priority,
                        syncAddress: (ushort)dmxData.SyncAddress));

                    destinationEP = GetUniverseEndPoint(dmxData.UniverseId.Value);
                    break;

                case DmxDataFrame.DataTypes.Sync:
                    packet = new SACNDataPacket(RootLayer.CreateRootLayerSync(
                        uuid: AcnId,
                        sequenceID: (byte)dmxData.Sequence,
                        syncAddress: (ushort)dmxData.SyncAddress));

                    destinationEP = GetUniverseEndPoint(dmxData.SyncAddress);
                    break;

                default:
                    return;
            }

            byte[] destinationMac = GetMacFromMulticastIP(destinationEP.Address);

            WritePacket(destinationMac, destinationEP, packet.ToArray(), dmxData.TimestampMS);
        }

        public void Footer(int universeId)
        {
            // Ignore
        }
    }
}
