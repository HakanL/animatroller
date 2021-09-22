using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Animatroller.Common;

namespace Animatroller.Processor.Command
{
    public class Generate : ICommand
    {
        private readonly ITransformer transformer;
        private readonly int[] universeIds;
        private readonly double frequency;
        private readonly long lengthFrames;
        private readonly byte fillValue;
        private double timestampMS;

        public Generate(ITransformer transformer, int[] universeIds, double frequency, long lengthFrames, byte fillValue)
        {
            this.transformer = transformer;
            this.universeIds = universeIds;
            this.frequency = frequency;
            this.lengthFrames = lengthFrames;
            this.fillValue = fillValue;
        }

        private InputFrame GetFrame()
        {
            var inputFrame = new InputFrame
            {
                TimestampMS = this.timestampMS
            };

            foreach (int universeId in this.universeIds)
            {
                var frame = new DmxDataFrame
                {
                    Data = new byte[512],
                    UniverseId = universeId,
                    SyncAddress = 0
                };

                for (int i = 0; i < 512; i++)
                    frame.Data[i] = this.fillValue;

                inputFrame.DmxData.Add(frame);
            }

            this.timestampMS += 1000.0 / this.frequency;

            return inputFrame;
        }

        public void Execute(TransformContext context)
        {
            var inputFrame = GetFrame();

            for (int frameCount = 0; frameCount < this.lengthFrames; frameCount++)
            {
                var nextFrame = GetFrame();

                this.transformer.Transform2(context, inputFrame, nextFrame);

                inputFrame = nextFrame;
            }
        }
    }
}
