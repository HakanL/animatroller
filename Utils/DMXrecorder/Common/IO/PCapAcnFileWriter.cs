using Haukcode.sACN.Model;
using System;
using System.Net;

namespace Animatroller.Common.IO
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

            // If multicast
            if (addr[0] == 239 && addr[1] == 255)
            {
                // Multicast
                byte[] mac = new byte[6];
                mac[0] = 0x01;
                mac[1] = 0;
                mac[2] = 0x5E;
                mac[3] = (byte)(addr[1] & 0x7F);
                mac[4] = addr[2];
                mac[5] = addr[3];

                return mac;
            }
            else
            {
                return new byte[6];
            }
        }

        public void Header(int universeId)
        {
            // Ignore
        }

        public static IPEndPoint GetUniverseEndPoint(int universe)
        {
            return new IPEndPoint(Haukcode.sACN.SACNCommon.GetMulticastAddress((ushort)universe), Haukcode.sACN.SACNCommon.SACN_PORT);
        }

        public void Output(DmxDataOutputPacket dmxData)
        {
            SACNDataPacket packet;
            IPEndPoint destinationEP;

            switch (dmxData.Content)
            {
                case DmxDataFrame dmxDataFrame:
                    packet = new SACNDataPacket(RootLayer.CreateRootLayerData(
                        uuid: AcnId,
                        sourceName: AcnSourceName,
                        universeID: (ushort)dmxDataFrame.UniverseId,
                        sequenceID: (byte)dmxData.Sequence,
                        data: dmxDataFrame.Data,
                        priority: this.priority,
                        syncAddress: (ushort)dmxDataFrame.SyncAddress));

                    if (dmxData.Content.Destination == null)
                        destinationEP = GetUniverseEndPoint(dmxDataFrame.UniverseId);
                    else
                        destinationEP = new IPEndPoint(dmxData.Content.Destination, Haukcode.sACN.SACNCommon.SACN_PORT);
                    break;

                case SyncFrame syncFrame:
                    packet = new SACNDataPacket(RootLayer.CreateRootLayerSync(
                        uuid: AcnId,
                        sequenceID: (byte)dmxData.Sequence,
                        syncAddress: (ushort)syncFrame.SyncAddress));

                    if (dmxData.Content.Destination == null)
                        destinationEP = GetUniverseEndPoint(syncFrame.SyncAddress);
                    else
                        destinationEP = new IPEndPoint(dmxData.Content.Destination, Haukcode.sACN.SACNCommon.SACN_PORT);
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
