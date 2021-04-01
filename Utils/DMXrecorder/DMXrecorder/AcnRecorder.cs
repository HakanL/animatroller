using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using Haukcode.sACN;

namespace Animatroller.DMXrecorder
{
    public class AcnRecorder : IRecorder
    {
        private readonly Guid acnId = new Guid("{1A246A28-D145-449F-B3F2-68676BA0E93F}");
        private double? clockOffset;
        private readonly Dictionary<int, UniverseData> universes;
        private readonly SACNClient sacnClient;
        private readonly OutputProcessor writer;
        private int receivedPackets;

        public AcnRecorder(OutputProcessor writer, int[] universes, IPAddress bindAddress)
        {
            if (universes.Length == 0)
                throw new ArgumentException("No universes specified");

            this.writer = writer;

            this.sacnClient = new SACNClient(
                senderId: this.acnId,
                senderName: "DMX Recorder",
                localAddress: bindAddress);

            this.sacnClient.OnError.Subscribe(ex => Console.WriteLine($"*Error* {ex.Message}"));
            this.sacnClient.OnPacket.Subscribe(AcnSocket_NewPacket);

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
            this.sacnClient.StartReceive();

            this.receivedPackets = 0;
            foreach (var kvp in this.universes)
                this.sacnClient.JoinDMXUniverse((ushort)kvp.Key);
        }

        public void StopRecord()
        {
            foreach (var kvp in this.universes)
            {
                this.sacnClient.DropDMXUniverse((ushort)kvp.Key);

                this.writer.CompleteUniverse(kvp.Key);
            }
        }

        private void AcnSocket_NewPacket(ReceiveDataPacket receiveData)
        {
            if (!this.clockOffset.HasValue)
                this.clockOffset = receiveData.TimestampMS;
            double timestamp = receiveData.TimestampMS - this.clockOffset.Value;

            var dataFramingLayer = receiveData.Packet.RootLayer?.FramingLayer as Haukcode.sACN.Model.DataFramingLayer;
            if (dataFramingLayer == null || dataFramingLayer.DMPLayer == null)
                return;

            var dmpLayer = dataFramingLayer.DMPLayer;

            if (dmpLayer.Length < 1)
                // Unknown/unsupported
                return;

            if (dmpLayer.StartCode != 0)
                // We only support start code 0
                return;

            if (!this.universes.TryGetValue(dataFramingLayer.UniverseId, out UniverseData universeData))
                // Unknown universe
                return;

            long sequence = receiveData.Packet.SequenceId + universeData.SequenceHigh;
            if (receiveData.Packet.SequenceId < universeData.LastSequenceLow)
            {
                // Wrap
                universeData.SequenceHigh += 256;
                sequence += 256;
            }
            universeData.LastSequenceLow = receiveData.Packet.SequenceId;

            var dmxData = RawDmxData.Create(
                millisecond: timestamp,
                sequence: sequence,
                universe: dataFramingLayer.UniverseId,
                data: dmpLayer.Data,
                syncAddress: dataFramingLayer.SyncAddress);

            this.writer.AddData(dmxData);

            this.receivedPackets++;
            if (this.receivedPackets % 100 == 0)
                Console.WriteLine($"Received {this.receivedPackets} packets");
        }

        public void Dispose()
        {
            this.sacnClient.Dispose();
        }
    }
}
