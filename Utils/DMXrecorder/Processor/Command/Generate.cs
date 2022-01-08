using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Animatroller.Common;

namespace Animatroller.Processor.Command
{
    public class Generate : ICommandOutput
    {
        private readonly int[] universeIds;
        private readonly double frequency;
        private readonly double durationMS;
        private readonly byte fillValue;
        private readonly GenerateSubCommands subCommand;
        private double timestampMS;
        private double rampUpPerFrame;
        private double currentValue;

        public enum GenerateSubCommands
        {
            Static,
            Ramp,
            Saw
        }

        public Generate(GenerateSubCommands subCommand, int[] universeIds, double frequency, double durationMS, byte fillValue = 0)
        {
            this.universeIds = universeIds ?? new int[] { 1 };
            this.frequency = frequency;
            this.durationMS = durationMS;
            this.fillValue = fillValue;
            this.subCommand = subCommand;

            int frames = (int)(this.durationMS / (1000.0 / this.frequency));

            switch (subCommand)
            {
                case GenerateSubCommands.Ramp:
                    this.rampUpPerFrame = 255.0 / frames;
                    break;

                case GenerateSubCommands.Saw:
                    this.rampUpPerFrame = 2 * 255.0 / frames;
                    break;
            }
        }

        private OutputFrame GetFrame()
        {
            var inputFrame = new InputFrame
            {
                TimestampMS = this.timestampMS,
                SyncAddress = 0
            };

            foreach (int universeId in this.universeIds)
            {
                var frame = new DmxDataFrame
                {
                    Data = new byte[512],
                    UniverseId = universeId,
                    SyncAddress = 0
                };

                if (this.subCommand == GenerateSubCommands.Static)
                {
                    for (int i = 0; i < 512; i++)
                        frame.Data[i] = this.fillValue;
                }
                else if (this.subCommand == GenerateSubCommands.Ramp)
                {
                    for (int i = 0; i < 512; i++)
                        frame.Data[i] = (byte)this.currentValue;

                    this.currentValue += this.rampUpPerFrame;
                }
                else if (this.subCommand == GenerateSubCommands.Saw)
                {
                    byte value = (byte)Math.Min(this.currentValue, 255);

                    for (int i = 0; i < 512; i++)
                        frame.Data[i] = value;

                    this.currentValue += this.rampUpPerFrame;

                    if (this.currentValue >= 255 || this.currentValue <= 0)
                        this.rampUpPerFrame = -this.rampUpPerFrame;
                }
                else
                    throw new ArgumentOutOfRangeException(nameof(subCommand));

                inputFrame.DmxData.Add(frame);
            }

            this.timestampMS += 1000.0 / this.frequency;

            return inputFrame;
        }

        public void Execute(ProcessorContext context, IOutputWriter outputWriter)
        {
            OutputFrame data;
            do
            {
                data = GetFrame();

                outputWriter.Output(context, data);

            } while (data.TimestampMS < this.durationMS);
        }
    }
}
