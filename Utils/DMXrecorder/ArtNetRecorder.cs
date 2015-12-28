using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Acn.Sockets;
using Acn.ArtNet;
using Acn.ArtNet.Sockets;
using Acn.ArtNet.Packets;

namespace Animatroller.DMXrecorder
{
    public class ArtNetRecorder : IRecorder
    {
        private Stopwatch timestamper;
        private Dictionary<int, UniverseData> universes;
        private ArtNetSocket socket;
        private FileWriter writer;

        public ArtNetRecorder(FileWriter writer, int[] universes)
        {
            if (universes.Length == 0)
                throw new ArgumentException("No universes specified");

            this.writer = writer;

            this.socket = new ArtNetSocket();
            this.socket.EnableBroadcast = true;

            this.socket.NewPacket += Socket_NewPacket;

            this.socket.Open(IPAddress.Any, IPAddress.Broadcast);

            this.timestamper = Stopwatch.StartNew();

            this.universes = new Dictionary<int, UniverseData>();

            foreach (int universe in universes)
            {
                var universeData = new UniverseData(universe);

                this.universes.Add(universe, universeData);

                this.writer.AddUniverse(universe);

                this.writer.AddData(universeData.GetInitData());
            }
        }

        public void StartRecord()
        {
        }

        public void StopRecord()
        {
        }

        private void Socket_NewPacket(object sender, NewPacketEventArgs<ArtNetPacket> e)
        {
            if (e.Packet.OpCode == ArtNetOpCodes.Dmx)
            {
                var packet = e.Packet as ArtNetDmxPacket;
                var newDmxData = packet.DmxData;

                UniverseData universeData;
                if (!this.universes.TryGetValue(packet.Universe, out universeData))
                    // Unknown universe
                    return;

                bool changed = false;
                for (int i = 0; i < Math.Min(universeData.LastDmxData.Length, newDmxData.Length); i++)
                {
                    if (universeData.LastDmxData[i] != newDmxData[i])
                    {
                        changed = true;
                    }

                    universeData.LastDmxData[i] = newDmxData[i];
                }

                long sequence = packet.Sequence + universeData.SequenceHigh;
                if (packet.Sequence < universeData.LastSequenceLow)
                {
                    // Wrap
                    universeData.SequenceHigh += 256;
                    sequence += 256;
                }
                universeData.LastSequenceLow = packet.Sequence;

                DmxData dmxData;
                if (!changed)
                {
                    dmxData = DmxData.CreateNoChange(this.timestamper.ElapsedMilliseconds, sequence, packet.Universe);
                }
                else
                {
                    dmxData = DmxData.CreateFullFrame(this.timestamper.ElapsedMilliseconds, sequence, packet.Universe,
                        newDmxData.ToArray());
                }

                this.writer.AddData(dmxData);
            }
        }

        public void Dispose()
        {
            this.socket.Close();
            this.socket.Dispose();
        }
    }
}
