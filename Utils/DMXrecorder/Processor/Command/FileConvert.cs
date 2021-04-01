using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Animatroller.Processor.Command
{
    public class FileConvert : ICommand
    {
        private readonly Common.IFileReader fileReader;
        private readonly Common.IFileWriter fileWriter;
        private readonly HashSet<int> universes;
        private readonly ITransformer transformer;

        public FileConvert(Common.IFileReader fileReader, Common.IFileWriter fileWriter, ITransformer transformer)
        {
            this.fileReader = fileReader;
            this.fileWriter = fileWriter;
            this.universes = new HashSet<int>();
            this.transformer = transformer;
        }

        public void Execute(TransformContext context)
        {
            double? timestampOffset = null;
            bool firstFrame = true;
            var readaheadQueue = new List<Common.DmxDataPacket>();

            while (this.fileReader.DataAvailable)
            {
                var data = this.fileReader.ReadFrame();

                if (data.DataType == Common.DmxDataFrame.DataTypes.Nop)
                    // Skip/null data
                    continue;

                if (!timestampOffset.HasValue)
                    timestampOffset = data.TimestampMS;

                // Apply offset
                data.TimestampMS -= timestampOffset.Value;

                readaheadQueue.Add(data);

                if (firstFrame && context.HasSyncFrames)
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
                                if (packet.UniverseId.HasValue && !this.universes.Contains(packet.UniverseId.Value))
                                {
                                    // Write header
                                    this.fileWriter.Header(packet.UniverseId.Value);
                                    this.universes.Add(packet.UniverseId.Value);
                                }

                                this.fileWriter.Output(packet);
                            });
                    }

                    readaheadQueue.Clear();
                }
            }

            // Write footers
            foreach (int universe in this.universes)
                this.fileWriter.Footer(universe);
        }
    }
}
