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
        private FileWriter writer;

        public AcnRecorder(FileWriter writer, int[] universes)
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

                this.writer.AddData(universeData.GetInitData());
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

            bool changed = false;
            for (int i = 0; i < Math.Min(universeData.LastDmxData.Length, newDmxData.Length - 1); i++)
            {
                if (universeData.LastDmxData[i] != newDmxData[i + 1])
                {
                    changed = true;
                }

                universeData.LastDmxData[i] = newDmxData[i + 1];
            }

            long sequence = e.Packet.Framing.SequenceNumber + universeData.SequenceHigh;
            if (e.Packet.Framing.SequenceNumber < universeData.LastSequenceLow)
            {
                // Wrap
                universeData.SequenceHigh += 256;
                sequence += 256;
            }
            universeData.LastSequenceLow = e.Packet.Framing.SequenceNumber;

            DmxData dmxData;
            if (!changed)
            {
                dmxData = DmxData.CreateNoChange(this.timestamper.ElapsedMilliseconds, sequence, e.Packet.Framing.Universe);
            }
            else
            {
                dmxData = DmxData.CreateFullFrame(this.timestamper.ElapsedMilliseconds, sequence, e.Packet.Framing.Universe,
                    newDmxData.Skip(1).ToArray());
            }

            this.writer.AddData(dmxData);
        }

        public void Dispose()
        {
            this.acnSocket.Close();
            this.acnSocket.Dispose();
        }
    }
}
