using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Animatroller.Common;

namespace Animatroller.Processor
{
    public class Transformer : ITransformer
    {
        private const double MinSeparationMS = 0.001;

        private readonly List<OutputFrame> output = new List<OutputFrame>();
        private OutputFrame currentFrame = null;
        private readonly IList<IBaseTransform> transforms;
        private readonly Dictionary<int, long> sequencePerUniverse = new Dictionary<int, long>();
        private readonly Dictionary<int, long> sequencePerSyncAddress = new Dictionary<int, long>();
        private readonly Common.IO.IFileWriter fileWriter;
        private int outputDuplicateCount;
        private double? firstFrameTimestampOffset = null;

        public Transformer(IList<IBaseTransform> transforms, Common.IO.IFileWriter fileWriter, int outputDuplicateCount)
        {
            this.transforms = transforms ?? new List<IBaseTransform>();
            this.fileWriter = fileWriter;
            this.outputDuplicateCount = outputDuplicateCount;
        }

        public void Simulate(TransformContext context, DmxDataPacket dmxData, Action<BaseDmxFrame> action)
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

        public void Transform(TransformContext context, Common.DmxDataPacket dmxData, Action<BaseDmxFrame> action)
        {
            var data = new List<BaseDmxFrame>() { dmxData.Content };

            foreach (var transform in this.transforms)
            {
                if (transform is ITransformData transformData)
                {
                    var outputData = new List<BaseDmxFrame>();

                    foreach (var input in data)
                    {
                        /*FIXME if (input.Data == null)
                        {
                            outputData.Add(input);
                        }
                        else
                        {*/
                        if (input is DmxDataFrame dmxDataFrame)
                        {
                            var newData = transformData.TransformData(dmxDataFrame);

                            if (newData == null)
                                outputData.Add(input);
                            else
                                outputData.AddRange(newData);
                        }
                        else
                        {
                            outputData.Add(input);
                        }
                        //}
                    }

                    data = outputData;
                }
            }

            double timestamp = dmxData.TimestampMS;

            foreach (var actionData in data)
            {
                foreach (var transform in this.transforms)
                {
                    if (transform is ITransformTimestamp transformTimestamp)
                    {
                        timestamp = Math.Round(transformTimestamp.TransformTimestamp(actionData, timestamp, context), 3);
                    }
                }

                if (actionData is DmxDataFrame dmxFrame)
                    action?.Invoke(dmxFrame);

                switch (actionData)
                {
                    case DmxDataFrame dmxDataFrame:
                        if (this.currentFrame == null)
                            this.currentFrame = new OutputFrame();

                        //FIXME per sync address
                        this.currentFrame.DmxData.Add(dmxDataFrame);
                        break;

                    case SyncFrame syncFrame:
                        if (this.currentFrame != null)
                        {
                            //FIXME this.currentFrame.TimestampMS = dmxData.TimestampMS;
                            this.currentFrame.SyncAddress = syncFrame.SyncAddress;

                            this.output.Add(this.currentFrame);
                            this.currentFrame = null;
                        }
                        break;
                }
            }
        }

        public void Transform2(TransformContext context, InputFrame inputFrame, InputFrame nextFrame, Action<BaseDmxFrame> action)
        {
            if (!this.firstFrameTimestampOffset.HasValue)
                this.firstFrameTimestampOffset = inputFrame.TimestampMS;
            double timestampMS = inputFrame.TimestampMS - this.firstFrameTimestampOffset.Value;
            double delayMS = 0;
            if (nextFrame != null)
                delayMS = nextFrame.TimestampMS - inputFrame.TimestampMS;

            var frame = new OutputFrame
            {
                DmxData = inputFrame.DmxData,
                SyncAddress = inputFrame.SyncAddress,
                DelayMS = delayMS
            };

            foreach (var transformTimestamp in this.transforms.OfType<ITransformTimestamp>())
            {
                frame.DelayMS = Math.Round(transformTimestamp.TransformTimestamp2(frame, context), 3);
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
                action?.Invoke(actionData);

                if (this.currentFrame == null)
                {
                    this.currentFrame = new OutputFrame
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

        public void WriteOutput()
        {
            if (!this.output.Any() || this.fileWriter == null)
                // Nothing
                return;

            var universeIds = this.output.SelectMany(x => x.DmxData.Select(x => x.UniverseId)).Distinct().ToList();

            // Write headers
            foreach (int universeId in universeIds)
                this.fileWriter.Header(universeId);

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
                        this.sequencePerUniverse.TryGetValue(dmxData.UniverseId, out sequence);

                        var packet = DmxDataOutputPacket.CreateFullFrame(
                            nextPacketWriteTimestampMS,
                            sequence,
                            dmxData.UniverseId,
                            dmxData.Data,
                            dmxData.SyncAddress);

                        this.fileWriter.Output(packet);
                        nextPacketWriteTimestampMS = packet.TimestampMS + MinSeparationMS;

                        sequence++;

                        this.sequencePerUniverse[dmxData.UniverseId] = sequence;

                        //timestamp += MinSeparationMS;
                    }

                    masterTimestamp += frame.DelayMS;

                    if (frame.SyncAddress != 0)
                    {
                        this.sequencePerSyncAddress.TryGetValue(frame.SyncAddress, out sequence);

                        var packet = DmxDataOutputPacket.CreateSync(masterTimestamp, sequence, frame.SyncAddress);
                        this.fileWriter.Output(packet);
                        // Pad the delay to the next set of data packets so we won't risk having the data come
                        // over the wire before the Sync packet during playback
                        // Not sure if it's necessary
                        nextPacketWriteTimestampMS = packet.TimestampMS + 1.0;

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
                this.fileWriter.Footer(universeId);
        }
    }
}
