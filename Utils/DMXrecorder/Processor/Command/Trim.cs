using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Animatroller.Common;

namespace Animatroller.Processor.Command
{
    public class Trim : ICommand
    {
        private readonly IInputReader inputReader;
        private readonly long trimStart;
        private readonly long? trimEnd;
        private readonly long? trimCount;
        private readonly ITransformer transformer;

        public Trim(IInputReader inputReader, long? trimStart, long? trimEnd, long? trimCount, ITransformer transformer)
        {
            if (trimStart <= 0)
                throw new ArgumentOutOfRangeException("TrimPos has to be a positive number (> 0)");

            this.inputReader = inputReader;
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
            var readaheadQueue = new List<Common.DmxDataOutputPacket>();

            while (true)
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

                var data = this.inputReader.ReadFrame();
                if (data == null)
                    break;

                int value;
                switch (data.Content)
                {
                    case DmxDataFrame dmxDataFrame:
                        inputFrameCountPerUniverse.TryGetValue(dmxDataFrame.UniverseId.Value, out value);
                        value++;
                        inputFrameCountPerUniverse[dmxDataFrame.UniverseId.Value] = value;
                        break;

                    case SyncFrame syncFrame:
                        inputFrameCountPerSyncAddress.TryGetValue(syncFrame.SyncAddress, out value);
                        value++;
                        inputFrameCountPerSyncAddress[syncFrame.SyncAddress] = value;
                        break;
                }

                if (inputPos >= trimStart)
                {
                    readaheadQueue.Add(data);

                    if (firstFrame)
                    {
                        // We need to add to the readahead queue to find the next sync
                        if (data.Content is SyncFrame)
                        {
                            context.FirstSyncTimestampMS = data.TimestampMS;

                            // Simulate the output so we can count the frames
                            context.FullFramesBeforeFirstSync = 0;
                            foreach (var item in readaheadQueue)
                            {
                                if (item.Content is DmxDataFrame)
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
                                switch (packet)
                                {
                                    case DmxDataFrame dmxDataFrame:
                                        outputFrameCountPerUniverse.TryGetValue(dmxDataFrame.UniverseId.Value, out value);
                                        value++;
                                        outputFrameCountPerUniverse[dmxDataFrame.UniverseId.Value] = value;
                                        break;
                                    
                                    case SyncFrame syncFrame:
                                        outputFrameCountPerSyncAddress.TryGetValue(syncFrame.SyncAddress, out value);
                                        value++;
                                        outputFrameCountPerSyncAddress[syncFrame.SyncAddress] = value;
                                        break;
                                }
                            });
                        }

                        readaheadQueue.Clear();
                    }
                }
            }
        }
    }
}
