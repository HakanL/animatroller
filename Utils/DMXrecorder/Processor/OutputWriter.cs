using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Animatroller.Common;

namespace Animatroller.Processor
{
    public class OutputWriter : IOutputWriter
    {
        private const double MinSeparationMS = 0.001;

        private readonly List<TransformFrame> output = new List<TransformFrame>();
        private TransformFrame currentFrame = null;
        private readonly IList<IBaseTransform> transforms;
        private readonly Dictionary<(System.Net.IPAddress Destination, int UniverseId), long> sequencePerUniverse = new Dictionary<(System.Net.IPAddress Destination, int UniverseId), long>();
        private readonly Dictionary<int, long> sequencePerSyncAddress = new Dictionary<int, long>();
        private int outputDuplicateCount;
        private double? firstFrameTimestampOffset = null;
        private bool removeSync;

        private OutputFrame inputFrameToProcess;
        private OutputFrame nextFrameToProcess;
        private Action<BaseDmxFrame> actionToProcess;
        private ProcessorContext contextToProcess;

        public OutputWriter(IList<IBaseTransform> transforms, Common.IO.IFileWriter fileWriter, int outputDuplicateCount, bool removeSync)
        {
            this.transforms = transforms ?? new List<IBaseTransform>();
            FileWriter = fileWriter;
            this.outputDuplicateCount = outputDuplicateCount;
            this.removeSync = removeSync;
        }

        public Common.IO.IFileWriter FileWriter { get; set; }

        public void Simulate(ProcessorContext context, DmxDataPacket dmxData, Action<BaseDmxFrame> action)
        {
            var data = new List<BaseDmxFrame>() { dmxData.Content };

            foreach (var transform in this.transforms)
            {
                if (transform is ITransformData transformData)
                {
                    var outputData = new List<BaseDmxFrame>();

                    foreach (var input in data)
                    {
                        if (input is DmxDataFrame dmxDataFrame)
                        {
                            var newData = transformData.TransformData(dmxDataFrame);

                            if (newData == null)
                                outputData.Add(input);
                            else
                                outputData.AddRange(newData);
                        }
                    }

                    data = outputData;
                }
            }

            foreach (var actionData in data)
            {
                DmxDataFrame packet;
                switch (actionData)
                {

                    //FIXME
                    case DmxDataFrame dmxDataFrame:
                        //case DmxDataFrame.DataTypes.FullFrame:
                        packet = new DmxDataFrame(dmxDataFrame);
                        break;

                    //case SyncFrame syncFrame:
                    //    packet = new DmxDataFrame(syncFrame);
                    //    break;

                    default:
                        continue;
                }

                action(packet);
            }
        }

        private void ProcessData()
        {
            if (this.inputFrameToProcess == null)
                // Nothing to do
                return;

            if (!this.firstFrameTimestampOffset.HasValue)
                this.firstFrameTimestampOffset = this.inputFrameToProcess.TimestampMS;
            double timestampMS = this.inputFrameToProcess.TimestampMS - this.firstFrameTimestampOffset.Value;
            double delayMS = 0;
            if (nextFrameToProcess != null)
                delayMS = nextFrameToProcess.TimestampMS - this.inputFrameToProcess.TimestampMS;

            var frame = new TransformFrame
            {
                DmxData = this.inputFrameToProcess.DmxData,
                SyncAddress = this.inputFrameToProcess.SyncAddress,
                DelayMS = delayMS
            };

            foreach (var transformTimestamp in this.transforms.OfType<ITransformTimestamp>())
            {
                frame.DelayMS = Math.Round(transformTimestamp.TransformTimestamp2(frame, this.contextToProcess), 3);
            }

            var data = new List<DmxDataFrame>(frame.DmxData);

            foreach (var transform in this.transforms)
            {
                if (transform is ITransformData transformData)
                {
                    var outputData = new List<DmxDataFrame>();

                    foreach (var input in data)
                    {
                        var newData = transformData.TransformData(input);

                        if (newData == null)
                            outputData.Add(input);
                        else
                            outputData.AddRange(newData);
                    }

                    data = outputData;
                }
            }

            foreach (var actionData in data)
            {
                this.actionToProcess?.Invoke(actionData);

                if (this.currentFrame == null)
                {
                    this.currentFrame = new TransformFrame
                    {
                        DelayMS = frame.DelayMS,
                        SyncAddress = frame.SyncAddress
                    };
                }

                this.currentFrame.DmxData.Add(actionData);
            }

            if (this.currentFrame != null)
            {
                //this.currentFrame.TimestampMS = timestampMS;
                //this.currentFrame.SyncAddress = frame.SyncAddress;
                //this.currentFrame.DelayMS = delayMS;

                this.output.Add(this.currentFrame);
                this.currentFrame = null;
            }
        }

        public void Output(ProcessorContext context, OutputFrame outputFrame, Action<BaseDmxFrame> action)
        {
            this.nextFrameToProcess = outputFrame;

            ProcessData();

            this.inputFrameToProcess = outputFrame;
            this.contextToProcess = context;
            this.actionToProcess = action;
            this.nextFrameToProcess = null;
        }

        public void WriteOutput()
        {
            ProcessData();

            if (!this.output.Any() || FileWriter == null)
                // Nothing
                return;

            var universeIds = this.output.SelectMany(x => x.DmxData.Select(x => x.UniverseId)).Distinct().ToList();

            if (universeIds.Count == 1)
                // No need for sync if we only have 1 universe
                this.removeSync = true;

            // Write headers
            foreach (int universeId in universeIds)
                FileWriter.Header(universeId);

            // Write output
            int loop = 0;
            double nextPacketWriteTimestampMS = 0;
            double masterTimestamp = 0;

            do
            {
                //TODO If we have multiple sync addresses then the output wouldn't be correct
                foreach (var frame in this.output)
                {
                    long sequence;

                    foreach (var dmxData in frame.DmxData.OrderBy(x => x.UniverseId))
                    {
                        this.sequencePerUniverse.TryGetValue((dmxData.Destination, dmxData.UniverseId), out sequence);

                        var packet = DmxDataOutputPacket.CreateFullFrame(
                            nextPacketWriteTimestampMS,
                            sequence,
                            dmxData.UniverseId,
                            dmxData.Destination,
                            dmxData.Data,
                            dmxData.SyncAddress);

                        FileWriter.Output(packet);
                        nextPacketWriteTimestampMS = packet.TimestampMS + MinSeparationMS;

                        sequence++;

                        this.sequencePerUniverse[(dmxData.Destination, dmxData.UniverseId)] = sequence;

                        //timestamp += MinSeparationMS;
                    }

                    masterTimestamp += frame.DelayMS;

                    if (frame.SyncAddress != 0 && !this.removeSync)
                    {
                        this.sequencePerSyncAddress.TryGetValue(frame.SyncAddress, out sequence);

                        // Send to all outputs
                        foreach (var destinationGroup in this.sequencePerUniverse.GroupBy(x => x.Key.Destination))
                        {
                            System.Net.IPAddress destination = destinationGroup.Key;
                            destination ??= Haukcode.sACN.SACNCommon.GetMulticastAddress((ushort)frame.SyncAddress);

                            var packet = DmxDataOutputPacket.CreateSync(masterTimestamp, sequence, frame.SyncAddress, destination);
                            FileWriter.Output(packet);

                            // Pad the delay to the next set of data packets so we won't risk having the data come
                            // over the wire before the Sync packet during playback
                            // Not sure if it's necessary
                            nextPacketWriteTimestampMS = packet.TimestampMS + 1.0;
                        }

                        sequence++;
                        this.sequencePerSyncAddress[frame.SyncAddress] = sequence;
                    }
                    else
                    {
                        nextPacketWriteTimestampMS = masterTimestamp;
                    }
                }
            } while (++loop < this.outputDuplicateCount);

            // Write footers
            foreach (int universeId in universeIds)
                FileWriter.Footer(universeId);
        }
    }
}
