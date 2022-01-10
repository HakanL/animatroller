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
        private readonly int frames;
        private double timestampMS;
        private double rampUpPerFrame;
        private double currentValue;

        public enum GenerateSubCommands
        {
            Static,
            Ramp,
            Saw,
            Rainbow
        }

        public Generate(GenerateSubCommands subCommand, int[] universeIds, double frequency, double durationMS, byte fillValue = 0)
        {
            this.universeIds = universeIds ?? new int[] { 1 };
            this.frequency = frequency;
            this.durationMS = durationMS;
            this.fillValue = fillValue;
            this.subCommand = subCommand;

            this.frames = (int)(this.durationMS / (1000.0 / this.frequency));

            switch (subCommand)
            {
                case GenerateSubCommands.Ramp:
                    this.rampUpPerFrame = 255.0 / this.frames;
                    break;

                case GenerateSubCommands.Saw:
                    this.rampUpPerFrame = 2 * 255.0 / this.frames;
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
                else if (this.subCommand == GenerateSubCommands.Rainbow)
                {
                    int numPixels = 170;
                    int writePos = 0;
                    double hue = this.currentValue;
                    double frac = 1.0 / this.frames;

                    for (int i = 0; i < numPixels; i++)
                    {
                        var rgb = HSL2RGB(hue, 1, 0.5);

                        hue += 0.01;
                        if (hue >= 1)
                            hue = 0;

                        frame.Data[writePos++] = rgb.R;
                        frame.Data[writePos++] = rgb.G;
                        frame.Data[writePos++] = rgb.B;
                    }
                    this.currentValue += 1.0 / this.frames;
                }
                else
                    throw new ArgumentOutOfRangeException(nameof(subCommand));

                inputFrame.DmxData.Add(frame);
            }

            this.timestampMS += 1000.0 / this.frequency;

            return inputFrame;
        }

        public struct RGB
        {
            public byte Red { get; set; }

            public byte Green { get; set; }

            public byte Blue { get; set; }
        }

        // Given H,S,L in range of 0-1
        // Returns a Color (RGB struct) in range of 0-255
        public static System.Drawing.Color HSL2RGB(double h, double sl, double l)
        {
            double v;
            double r, g, b;

            r = l;   // default to gray
            g = l;
            b = l;
            v = (l <= 0.5) ? (l * (1.0 + sl)) : (l + sl - l * sl);
            if (v > 0)
            {
                double m;
                double sv;
                int sextant;
                double fract, vsf, mid1, mid2;

                m = l + l - v;
                sv = (v - m) / v;
                h *= 6.0;
                sextant = (int)h;
                fract = h - sextant;
                vsf = v * sv * fract;
                mid1 = m + vsf;
                mid2 = v - vsf;
                switch (sextant)
                {
                    case 0:
                        r = v;
                        g = mid1;
                        b = m;
                        break;
                    case 1:
                        r = mid2;
                        g = v;
                        b = m;
                        break;
                    case 2:
                        r = m;
                        g = v;
                        b = mid1;
                        break;
                    case 3:
                        r = m;
                        g = mid2;
                        b = v;
                        break;
                    case 4:
                        r = mid1;
                        g = m;
                        b = v;
                        break;
                    case 5:
                        r = v;
                        g = m;
                        b = mid2;
                        break;
                }
            }

            return System.Drawing.Color.FromArgb(
                (byte)(r * 255.0f),
                (byte)(g * 255.0f),
                (byte)(b * 255.0f));
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
