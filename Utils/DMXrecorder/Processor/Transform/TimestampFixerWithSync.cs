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
        private double? adjustedTimingMS;
        private double adjustTolerancePercent;

        public TimestampFixerWithSync(double? adjustedTimingMS, double adjustTolerancePercent)
        {
            this.adjustedTimingMS = adjustedTimingMS;
            this.adjustTolerancePercent = adjustTolerancePercent;
        }

        public double TransformTimestamp(Common.BaseDmxData dmxData, double timestampMS, TransformContext context)
        {
            double newTimestamp = timestampMS;
            bool firstSync = false;
            if (dmxData.DataType == BaseDmxData.DataTypes.Sync && !this.timestampOffset.HasValue)
            {
                this.timestampOffset = timestampMS - MinSeparationMS * context.FullFramesBeforeFirstSync;
                firstSync = true;
            }

            switch (dmxData.DataType)
            {
                case BaseDmxData.DataTypes.FullFrame:
                    newTimestamp = MinSeparationMS * this.universeFrameCount;
                    this.universeFrameCount++;
                    break;

                case BaseDmxData.DataTypes.Sync:
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
