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

namespace Animatroller.DMXrecorder
{
    public class AcnRecorder : IRecorder
    {
        private readonly Guid acnId = new Guid("{1A246A28-D145-449F-B3F2-68676BA0E93F}");
        private Stopwatch timestamper;
        private Dictionary<int, UniverseData> universes;
        private Acn.Sockets.StreamingAcnSocket acnSocket;
        private OutputProcessor writer;

        public AcnRecorder(OutputProcessor writer, int[] universes)
        {
            if (universes.Length == 0)
                throw new ArgumentException("No universes specified");

            this.writer = writer;

            this.acnSocket = new Acn.Sockets.StreamingAcnSocket(acnId, "DMX Recorder");

            this.acnSocket.NewPacket += AcnSocket_NewPacket;

            this.acnSocket.Open(IPAddress.Any);

            this.timestamper = Stopwatch.StartNew();

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
            foreach (var kvp in this.universes)
                this.acnSocket.JoinDmxUniverse(kvp.Key);
        }

        public void StopRecord()
        {
            foreach (var kvp in this.universes)
                this.acnSocket.DropDmxUniverse(kvp.Key);
        }

        private void AcnSocket_NewPacket(object sender, Acn.Sockets.NewPacketEventArgs<Acn.Packets.sAcn.StreamingAcnDmxPacket> e)
        {
            var propData = e.Packet.Dmx.PropertyData;
            if (propData.Length < 1)
                // Unknown/unsupported
                return;

            if (propData[0] != 0)
                // We only support start code 0
                return;

            var newDmxData = e.Packet.Dmx.PropertyData;

            UniverseData universeData;
            if (!this.universes.TryGetValue(e.Packet.Framing.Universe, out universeData))
                // Unknown universe
                return;

            long sequence = e.Packet.Framing.SequenceNumber + universeData.SequenceHigh;
            if (e.Packet.Framing.SequenceNumber < universeData.LastSequenceLow)
            {
                // Wrap
                universeData.SequenceHigh += 256;
                sequence += 256;
            }
            universeData.LastSequenceLow = e.Packet.Framing.SequenceNumber;

            var dmxData = RawDmxData.Create(
                millisecond: this.timestamper.ElapsedMilliseconds,
                sequence: sequence,
                universe: e.Packet.Framing.Universe,
                data: newDmxData.Skip(1).ToArray());

            this.writer.AddData(dmxData);
        }

        public void Dispose()
        {
            this.acnSocket.Close();
            this.acnSocket.Dispose();
        }
    }
}
