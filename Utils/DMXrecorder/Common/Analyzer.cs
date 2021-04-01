using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Animatroller.Common
{
    public class Analyzer
    {
        private readonly Common.IFileReader fileReader;
        private int readFrames;
        private bool syncFrameDetected;
        private readonly Dictionary<int, double> lastFrameTimestampPerUniverse = new Dictionary<int, double>();
        private readonly Dictionary<int, double> lastFrameTimestampPerSyncAddress = new Dictionary<int, double>();
        private readonly List<double> intervals = new List<double>();
        private readonly List<double> syncIntervals = new List<double>();
        private double? firstSyncTimestampMS;

        public Analyzer(Common.IFileReader fileReader)
        {
            this.fileReader = fileReader;
        }

        public int ReadFrames => this.readFrames;

        public bool SyncFrameDetected => this.syncFrameDetected;

        public double FirstSyncTimestampMS => this.firstSyncTimestampMS ?? 0;

        public int? AdjustedFrequency { get; set; } = null;

        public double? AdjustedTiming { get; set; } = null;

        public double? DetectedFrequency { get; set; } = null;

        public double? AverageTiming { get; set; } = null;

        public void Analyze(int maxFramesToRead = 1000)
        {
            while (this.fileReader.DataAvailable && readFrames <= maxFramesToRead)
            {
                var dmxFrame = this.fileReader.ReadFrame();
                if (dmxFrame == null)
                    break;
                readFrames++;

                double lastFrameTimestamp;
                double interval;

                if (dmxFrame.DataType == Common.DmxDataFrame.DataTypes.Sync)
                {
                    if (!this.firstSyncTimestampMS.HasValue)
                        this.firstSyncTimestampMS = dmxFrame.TimestampMS;

                    this.syncFrameDetected = true;

                    this.lastFrameTimestampPerSyncAddress.TryGetValue(dmxFrame.SyncAddress, out lastFrameTimestamp);
                    this.lastFrameTimestampPerSyncAddress[dmxFrame.SyncAddress] = dmxFrame.TimestampMS;
                    interval = Math.Round(dmxFrame.TimestampMS - lastFrameTimestamp, 2);
                    if (interval > 0)
                        syncIntervals.Add(interval);
                }
                else
                {
                    this.lastFrameTimestampPerUniverse.TryGetValue(dmxFrame.UniverseId.Value, out lastFrameTimestamp);
                    this.lastFrameTimestampPerUniverse[dmxFrame.UniverseId.Value] = dmxFrame.TimestampMS;
                    interval = Math.Round(dmxFrame.TimestampMS - lastFrameTimestamp, 2);
                    if (interval > 0)
                        intervals.Add(interval);
                }
            }

            intervals.Sort();
            syncIntervals.Sort();

            if (this.syncFrameDetected && syncIntervals.Any())
            {
                Console.WriteLine($"Sync frames detected in input file. Sampled {syncIntervals.Count} sync-frames");

                double stdDev = StdDev(syncIntervals, false);
                double avg = syncIntervals.Average();
                Console.WriteLine($"Standard deviation = {stdDev:N3} ms, average unscrubbed = {avg:N3} ms");

                // Remove any outside the standard deviation
                int removedOutliers = 0;
                for (int i = syncIntervals.Count - 1; i >= 0; i--)
                {
                    if (Math.Abs(syncIntervals[i] - avg) > stdDev)
                    {
                        // Remove
                        syncIntervals.RemoveAt(i);
                        removedOutliers++;
                    }
                }
                Console.WriteLine($"Removing {removedOutliers} outliers");

                // Re-calculate the average
                AverageTiming = syncIntervals.Average();
                DetectedFrequency = 1000.0 / AverageTiming.Value;
                AdjustedFrequency = (int)Math.Round(DetectedFrequency.Value, 0);
                AdjustedTiming = 1000.0 / AdjustedFrequency.Value;

                Console.WriteLine($"Detected Freq = {DetectedFrequency:N2} Hz and average timing (scrubbed) = {AverageTiming:N2} ms");
                Console.WriteLine($"Adjusted frequency set to {AdjustedFrequency:N2} Hz and timing to {AdjustedTiming:N2} ms");
            }
        }

        // Return the standard deviation of an array of Doubles.
        //
        // If the second argument is True, evaluate as a sample.
        // If the second argument is False, evaluate as a population.
        public static double StdDev(IEnumerable<double> values, bool as_sample)
        {
            // Get the mean.
            double mean = values.Sum() / values.Count();

            // Get the sum of the squares of the differences
            // between the values and the mean.
            var squares_query =
                from double value in values
                select (value - mean) * (value - mean);
            double sum_of_squares = squares_query.Sum();

            if (as_sample)
            {
                return Math.Sqrt(sum_of_squares / (values.Count() - 1));
            }
            else
            {
                return Math.Sqrt(sum_of_squares / values.Count());
            }
        }
    }
}
