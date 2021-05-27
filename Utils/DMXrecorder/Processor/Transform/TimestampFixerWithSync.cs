using System;
using System.Collections.Generic;
using System.Text;
using Animatroller.Common;

namespace Animatroller.Processor.Transform
{
    public class TimestampFixerWithSync : ITransformTimestamp
    {
        private const double MinSeparationMS = 0.001;

        private double? timestampOffset;
        private double masterClock;
        private double lastTimestamp;
        private int universeFrameCount;
        private readonly double? adjustedTimingMS;
        private readonly double adjustTolerancePercent;

        public TimestampFixerWithSync(double? adjustedTimingMS, double adjustTolerancePercent)
        {
            this.adjustedTimingMS = adjustedTimingMS;
            this.adjustTolerancePercent = adjustTolerancePercent;
        }

        public double TransformTimestamp2(Common.OutputFrame frame, TransformContext context)
        {
            double delayMS = this.adjustedTimingMS ?? frame.DelayMS;

            return delayMS;
        }

        public double TransformTimestamp(Common.BaseDmxFrame dmxData, double timestampMS, TransformContext context)
        {
            double newTimestamp = timestampMS;
            bool firstSync = false;
            if (dmxData is SyncFrame && !this.timestampOffset.HasValue)
            {
                this.timestampOffset = timestampMS;//FIXME - MinSeparationMS * context.FullFramesBeforeFirstSync;
                firstSync = true;
            }

            switch (dmxData)
            {
                case DmxDataFrame dmxDataFrame:
                    newTimestamp = MinSeparationMS * this.universeFrameCount;
                    this.universeFrameCount++;
                    break;

                case SyncFrame syncFrame:
                    // Reset the counter
                    this.universeFrameCount = 0;
                    newTimestamp -= this.timestampOffset ?? 0;

                    double durationSinceLastSync = newTimestamp - this.lastTimestamp;
                    this.lastTimestamp = newTimestamp;
                    if (this.adjustedTimingMS.HasValue && !firstSync)
                    {
                        double driftMS = Math.Abs(durationSinceLastSync - this.adjustedTimingMS.Value);

                        // If we're within 10% of the adjusted timing then we should use that instead
                        if (driftMS <= (this.adjustedTimingMS.Value * this.adjustTolerancePercent / 100.0))
                        {
                            newTimestamp = this.masterClock + this.adjustedTimingMS.Value - MinSeparationMS;
                        }
                        else
                        {
                            double driftPercentage = driftMS / this.adjustedTimingMS.Value;
                            Console.WriteLine($"Timestamp {timestampMS:N1} had drift of {driftMS:N1} ({driftPercentage:P1}), not correcting");
                        }
                    }

                    this.masterClock = Math.Round(newTimestamp + MinSeparationMS, 3);
                    newTimestamp = -MinSeparationMS;
                    break;
            }

            return this.masterClock + newTimestamp;
        }
    }
}
