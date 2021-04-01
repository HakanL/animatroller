using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Animatroller.Processor.Command
{
    //FIXME: Make it work with multiple universes

    public class Trim : ICommand
    {
        private readonly Common.IFileReader fileReader;
        private readonly Common.IFileWriter fileWriter;
        private readonly long trimStart;
        private readonly long? trimEnd;
        private readonly long? trimCount;
        private readonly ITransformer transformer;

        public Trim(Common.IFileReader fileReader, Common.IFileWriter fileWriter, long? trimStart, long? trimEnd, long? trimCount, ITransformer transformer)
        {
            if (trimStart <= 0)
                throw new ArgumentOutOfRangeException("TrimPos has to be a positive number (> 0)");

            this.fileReader = fileReader;
            this.fileWriter = fileWriter;
            this.trimStart = trimStart ?? 0;
            this.trimEnd = trimEnd;
            this.trimCount = trimCount;
            this.transformer = transformer;
        }

        public void Execute(TransformContext context)
        {
            bool firstFrame = true;
            var inputFrameCountPerUniverse = new Dictionary<int, int>();
            var inputFrameCountPerSyncAddress = new Dictionary<int, int>();
            var outputFrameCountPerUniverse = new Dictionary<int, int>();
            var outputFrameCountPerSyncAddress = new Dictionary<int, int>();
            var readaheadQueue = new List<Common.DmxDataPacket>();

            while (this.fileReader.DataAvailable)
            {
                int inputPos;
                if (inputFrameCountPerSyncAddress.Any())
                {
                    // If we have sync packets then we should use that
                    inputPos = inputFrameCountPerSyncAddress.Max(x => (int?)x.Value) ?? 0;
                }
                else
                {
                    inputPos = inputFrameCountPerUniverse.Max(x => (int?)x.Value) ?? 0;
                }

                // Check if we have passed the threshold
                if (trimEnd.HasValue && inputPos >= trimEnd.Value)
                    break;

                int outputCount;
                if (outputFrameCountPerSyncAddress.Any())
                {
                    // If we have sync packets then we should use that
                    outputCount = outputFrameCountPerSyncAddress.Max(x => (int?)x.Value) ?? 0;
                }
                else
                {
                    outputCount = outputFrameCountPerUniverse.Max(x => (int?)x.Value) ?? 0;
                }

                // Check if we have passed the threshold
                if (trimCount.HasValue && outputCount >= trimCount.Value)
                    break;

                var data = this.fileReader.ReadFrame();

                int value;
                switch (data.DataType)
                {
                    case Common.BaseDmxData.DataTypes.FullFrame:
                        inputFrameCountPerUniverse.TryGetValue(data.UniverseId.Value, out value);
                        value++;
                        inputFrameCountPerUniverse[data.UniverseId.Value] = value;
                        break;

                    case Common.BaseDmxData.DataTypes.Sync:
                        inputFrameCountPerSyncAddress.TryGetValue(data.SyncAddress, out value);
                        value++;
                        inputFrameCountPerSyncAddress[data.SyncAddress] = value;
                        break;
                }

                if (inputPos >= trimStart)
                {
                    readaheadQueue.Add(data);

                    if (firstFrame)
                    {
                        // We need to add to the readahead queue to find the next sync
                        if (data.DataType == Common.BaseDmxData.DataTypes.Sync)
                        {
                            context.FirstSyncTimestampMS = data.TimestampMS;

                            // Simulate the output so we can count the frames
                            context.FullFramesBeforeFirstSync = 0;
                            foreach (var item in readaheadQueue)
                            {
                                if (item.DataType == Common.BaseDmxData.DataTypes.FullFrame)
                                    this.transformer.Simulate(context, item, packet => context.FullFramesBeforeFirstSync++);
                            }

                            firstFrame = false;
                        }
                    }
                    else
                    {
                        foreach (var outputData in readaheadQueue)
                        {
                            this.transformer.Transform(context, outputData, packet =>
                            {
                                this.fileWriter.Output(packet);
                            });

                            switch (outputData.DataType)
                            {
                                case Common.BaseDmxData.DataTypes.FullFrame:
                                    outputFrameCountPerUniverse.TryGetValue(outputData.UniverseId.Value, out value);
                                    value++;
                                    outputFrameCountPerUniverse[outputData.UniverseId.Value] = value;
                                    break;

                                case Common.BaseDmxData.DataTypes.Sync:
                                    outputFrameCountPerSyncAddress.TryGetValue(outputData.SyncAddress, out value);
                                    value++;
                                    outputFrameCountPerSyncAddress[outputData.SyncAddress] = value;
                                    break;
                            }
                        }

                        readaheadQueue.Clear();
                    }
                }
            }
        }
    }
}
