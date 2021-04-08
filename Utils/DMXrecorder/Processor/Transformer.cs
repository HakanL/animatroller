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

        public class Frame
        {
            public double TimestampMS;

            public int? SyncAddress;

            public List<DmxDataFrame> DmxData;

            public Frame()
            {
                DmxData = new List<DmxDataFrame>();
            }
        }

        private readonly List<Frame> output = new List<Frame>();
        private Frame currentFrame = null;
        private readonly IList<IBaseTransform> transforms;
        private readonly Dictionary<int, long> sequencePerUniverse = new Dictionary<int, long>();
        private readonly Dictionary<int, long> sequencePerSyncAddress = new Dictionary<int, long>();
        private readonly Common.IO.IFileWriter fileWriter;
        private int outputDuplicateCount;

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
                            this.currentFrame = new Frame();

                        //FIXME per sync address
                        this.currentFrame.DmxData.Add(dmxDataFrame);
                        break;

                    case SyncFrame syncFrame:
                        if (this.currentFrame != null)
                        {
                            this.currentFrame.TimestampMS = dmxData.TimestampMS;
                            this.currentFrame.SyncAddress = syncFrame.SyncAddress;

                            this.output.Add(this.currentFrame);
                            this.currentFrame = null;
                        }
                        break;
                }
            }
        }

        public void WriteOutput()
        {
            var universeIds = this.output.SelectMany(x => x.DmxData.Where(x => x.UniverseId.HasValue).Select(x => x.UniverseId.Value)).Distinct().ToList();

            // Write headers
            foreach (int universeId in universeIds)
                this.fileWriter.Header(universeId);

            // Write output
            int loop = 0;
            do
            {
                foreach (var data in this.output)
                {
                    double timestamp = data.TimestampMS - (data.DmxData.Count * MinSeparationMS);

                    foreach (var dmxData in data.DmxData.OrderBy(x => x.UniverseId))
                    {
                        long sequence = 0;
                        if (dmxData.UniverseId.HasValue)
                            this.sequencePerUniverse.TryGetValue(dmxData.UniverseId.Value, out sequence);
                        else
                            this.sequencePerSyncAddress.TryGetValue(dmxData.SyncAddress, out sequence);

                        DmxDataOutputPacket packet;
                        /*FIXME switch (dmxData.Content)
                        {
                            case DmxDataFrame dmxDataFrame:
                                packet = new DmxDataOutputPacket(dmxData, timestamp, sequence);
                                break;

                            case SyncFrame syncFrame:
                                packet = new DmxDataOutputPacket(dmxData, timestamp, sequence);
                                break;

                            default:
                                continue;
                        }
                        this.fileWriter.Output(packet);*/

                        sequence++;

                        if (dmxData.UniverseId.HasValue)
                            this.sequencePerUniverse[dmxData.UniverseId.Value] = sequence;
                        else
                            this.sequencePerSyncAddress[dmxData.SyncAddress] = sequence;
                    }
                }
            } while (++loop < this.outputDuplicateCount);

            // Write footers
            foreach (int universeId in universeIds)
                this.fileWriter.Footer(universeId);
        }
    }
}
