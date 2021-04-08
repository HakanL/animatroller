using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Animatroller.Common
{
    public class Analyzer
    {
        private readonly IInputReader inputReader;
        private int readFrames;
        private bool syncFrameDetected;
        private readonly Dictionary<int, double> lastFrameTimestampPerUniverse = new Dictionary<int, double>();
        private readonly Dictionary<int, double> lastFrameTimestampPerSyncAddress = new Dictionary<int, double>();
        private readonly List<double> intervals = new List<double>();
        private readonly List<double> syncIntervals = new List<double>();
        private double? firstSyncTimestampMS;

        public Analyzer(IInputReader inputReader)
        {
            this.inputReader = inputReader;
        }

        public int ReadFrames => this.readFrames;

        public bool SyncFrameDetected => this.syncFrameDetected;

        public double FirstSyncTimestampMS => this.firstSyncTimestampMS ?? 0;

        public int? AdjustedFrequency { get; set; } = null;

        public double? AdjustedIntervalMS { get; set; } = null;

        public double? DetectedFrequency { get; set; } = null;

        public double? AverageIntervalMS { get; set; } = null;

        public double? ShortestIntervalMS { get; set; } = null;

        public int? ShortestFrequency { get; set; } = null;

        public bool? IsOptimizedStream
        {
            get
            {
                if (!ShortestIntervalMS.HasValue || !AverageIntervalMS.HasValue)
                    // Unknown
                    return null;

                // If shortest interval is less than half of the average then we have an optimized stream and should use the shortest interval
                return ShortestIntervalMS.Value * 2 < AverageIntervalMS.Value;
            }
        }

        public void Analyze(int maxFramesToRead = 1000)
        {
            while (readFrames <= maxFramesToRead)
            {
                var dmxFrame = this.inputReader.ReadFrame();
                if (dmxFrame == null)
                    break;

                readFrames++;

                double lastFrameTimestamp;
                double interval;

                if (dmxFrame.Content is SyncFrame syncFrame)
                {
                    if (!this.firstSyncTimestampMS.HasValue)
                        this.firstSyncTimestampMS = dmxFrame.TimestampMS;

                    this.syncFrameDetected = true;

                    this.lastFrameTimestampPerSyncAddress.TryGetValue(syncFrame.SyncAddress, out lastFrameTimestamp);
                    this.lastFrameTimestampPerSyncAddress[syncFrame.SyncAddress] = dmxFrame.TimestampMS;
                    interval = Math.Round(dmxFrame.TimestampMS - lastFrameTimestamp, 2);
                    if (interval > 0)
                        syncIntervals.Add(interval);
                }
                else if (dmxFrame.Content is DmxDataFrame dmxDataFrame)
                {
                    this.lastFrameTimestampPerUniverse.TryGetValue(dmxDataFrame.UniverseId.Value, out lastFrameTimestamp);
                    this.lastFrameTimestampPerUniverse[dmxDataFrame.UniverseId.Value] = dmxFrame.TimestampMS;
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

                ShortestIntervalMS = syncIntervals.First();
                ShortestFrequency = (int)Math.Round(1000.0 / ShortestIntervalMS.Value, 0);
                double stdDev = StdDev(syncIntervals, false);
                AverageIntervalMS = syncIntervals.Average();

                Console.WriteLine($"Shortest period = {ShortestIntervalMS:N3} ms, frequency = {ShortestFrequency:N1} Hz");
                Console.WriteLine($"Standard deviation = {stdDev:N3} ms, average unscrubbed = {AverageIntervalMS:N3} ms");
                Console.WriteLine($"Optimized stream = {(IsOptimizedStream == true ? "Yes" : "No")}");

                if (IsOptimizedStream == false)
                {
                    // Remove any outside the standard deviation
                    int removedOutliers = 0;
                    for (int i = syncIntervals.Count - 1; i >= 0; i--)
                    {
                        if (Math.Abs(syncIntervals[i] - AverageIntervalMS.Value) > stdDev)
                        {
                            // Remove
                            syncIntervals.RemoveAt(i);
                            removedOutliers++;
                        }
                    }
                    Console.WriteLine($"Removing {removedOutliers} outliers");

                    // Re-calculate the average
                    AverageIntervalMS = syncIntervals.Average();
                    DetectedFrequency = 1000.0 / AverageIntervalMS.Value;
                    AdjustedFrequency = (int)Math.Round(DetectedFrequency.Value, 0);
                    AdjustedIntervalMS = 1000.0 / AdjustedFrequency.Value;

                    Console.WriteLine($"Detected Freq = {DetectedFrequency:N2} Hz and average timing (scrubbed) = {AverageIntervalMS:N2} ms");
                    Console.WriteLine($"Adjusted frequency set to {AdjustedFrequency:N1} Hz and timing to {AdjustedIntervalMS:N2} ms");
                }
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
