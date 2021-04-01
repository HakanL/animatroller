using System;
using System.Collections.Generic;
using System.Text;
using Animatroller.Common;

namespace Animatroller.Processor
{
    public class Transformer : ITransformer
    {
        private readonly IList<IBaseTransform> transforms;
        private readonly Dictionary<int, long> sequencePerUniverse = new Dictionary<int, long>();
        private readonly Dictionary<int, long> sequencePerSyncAddress = new Dictionary<int, long>();

        public Transformer(IList<IBaseTransform> transforms)
        {
            this.transforms = transforms ?? new List<IBaseTransform>();
        }

        public void Simulate(TransformContext context, DmxDataFrame dmxData, Action<DmxDataPacket> action)
        {
            var data = new List<BaseDmxData>() { dmxData };

            foreach (var transform in this.transforms)
            {
                if (transform is ITransformData transformData)
                {
                    var outputData = new List<BaseDmxData>();

                    foreach (var input in data)
                    {
                        if (input.Data == null)
                        {
                            outputData.Add(input);
                        }
                        else
                        {
                            var newData = transformData.TransformData(input);

                            if (newData == null)
                                outputData.Add(input);
                            else
                                outputData.AddRange(newData);
                        }
                    }

                    data = outputData;
                }
            }

            double timestamp = dmxData.TimestampMS;

            foreach (var actionData in data)
            {
                DmxDataPacket packet;
                switch (actionData.DataType)
                {
                    case BaseDmxData.DataTypes.FullFrame:
                        packet = new DmxDataPacket(actionData, timestamp, -1);
                        break;

                    case BaseDmxData.DataTypes.Sync:
                        packet = new DmxDataPacket(actionData, timestamp, -1);
                        break;

                    default:
                        continue;
                }

                action(packet);
            }
        }

        public void Transform(TransformContext context, Common.DmxDataFrame dmxData, Action<Common.DmxDataPacket> action)
        {
            var data = new List<BaseDmxData>() { dmxData };

            foreach (var transform in this.transforms)
            {
                if (transform is ITransformData transformData)
                {
                    var outputData = new List<BaseDmxData>();

                    foreach (var input in data)
                    {
                        if (input.Data == null)
                        {
                            outputData.Add(input);
                        }
                        else
                        {
                            var newData = transformData.TransformData(input);

                            if (newData == null)
                                outputData.Add(input);
                            else
                                outputData.AddRange(newData);
                        }
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

                long sequence = 0;
                if (actionData.UniverseId.HasValue)
                    this.sequencePerUniverse.TryGetValue(actionData.UniverseId.Value, out sequence);
                else
                    this.sequencePerSyncAddress.TryGetValue(actionData.SyncAddress, out sequence);

                DmxDataPacket packet;
                switch (actionData.DataType)
                {
                    case BaseDmxData.DataTypes.FullFrame:
                        packet = new DmxDataPacket(actionData, timestamp, sequence);
                        break;

                    case BaseDmxData.DataTypes.Sync:
                        packet = new DmxDataPacket(actionData, timestamp, sequence);
                        break;

                    default:
                        continue;
                }

                action(packet);

                sequence++;

                if (actionData.UniverseId.HasValue)
                    this.sequencePerUniverse[actionData.UniverseId.Value] = sequence;
                else
                    this.sequencePerSyncAddress[actionData.SyncAddress] = sequence;
            }
        }
    }
}
