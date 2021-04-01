using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using Haukcode.ArtNet;
using Haukcode.ArtNet.Packets;
using Haukcode.ArtNet.Sockets;
using Haukcode.Sockets;

namespace Animatroller.DMXrecorder
{
    public class ArtNetRecorder : IRecorder
    {
        private Stopwatch timestamper;
        private Dictionary<int, UniverseData> universes;
        private ArtNetSocket socket;
        private OutputProcessor writer;

        public ArtNetRecorder(OutputProcessor writer, int[] universes)
        {
            if (universes.Length == 0)
                throw new ArgumentException("No universes specified");

            this.writer = writer;

            this.socket = new ArtNetSocket();
            this.socket.EnableBroadcast = true;

            this.socket.NewPacket += Socket_NewPacket;

            this.socket.Open(IPAddress.Any, IPAddress.Broadcast);

            this.universes = new Dictionary<int, UniverseData>();

            foreach (int universe in universes)
            {
                var universeData = new UniverseData(universe);

                this.universes.Add(universe, universeData);

                this.writer.AddUniverse(universe);
            }
        }

        public void StartRecord()
        {
        }

        public void StopRecord()
        {
            foreach (var kvp in this.universes)
            {
                this.writer.CompleteUniverse(kvp.Key);
            }
        }

        private void Socket_NewPacket(object sender, NewPacketEventArgs<ArtNetPacket> e)
        {
            if (e.Packet.OpCode == ArtNetOpCodes.Dmx)
            {
                if (this.timestamper == null)
                    this.timestamper = Stopwatch.StartNew();

                var packet = e.Packet as ArtNetDmxPacket;

                if (!this.universes.ContainsKey(packet.Universe))
                    // Unknown universe
                    return;

                var dmxData = RawDmxData.Create(
                    millisecond: this.timestamper.ElapsedMilliseconds,
                    sequence: packet.Sequence,
                    universe: packet.Universe,
                    data: packet.DmxData,
                    syncAddress: 0);

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
