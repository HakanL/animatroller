using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Animatroller.Processor.Command
{
    public class FindLoop : ICommandInput
    {
        private readonly bool trimBlack;
        private readonly bool firstFrameBlack;

        public FindLoop(bool trimBlack, bool firstFrameBlack)
        {
            this.trimBlack = trimBlack;
            this.firstFrameBlack = firstFrameBlack;
        }

        public void Execute(ProcessorContext context, Common.IInputReader inputReader)
        {
            double? startBlackTimestamp = null;
            double? endBlackTimestamp = null;
            double lastTimestamp = 0;
            long? firstPosition = null;
            double firstTimestamp = 0;

            // Find all universes and the first data in each
            var firstData = new Dictionary<int, byte[]>();
            var currentData = new Dictionary<int, byte[]>();

            var diffList = new List<(double Match, long Position, double TimestampMS)>();

            Common.InputFrame data;
            if (this.trimBlack)
            {
                while ((data = inputReader.ReadFrame()) != null)
                {
                    if (data.IsAllBlack())
                    {
                        if (startBlackTimestamp.HasValue && !endBlackTimestamp.HasValue)
                            endBlackTimestamp = data.TimestampMS;
                    }
                    else
                    {
                        if (!startBlackTimestamp.HasValue)
                            startBlackTimestamp = lastTimestamp;

                        endBlackTimestamp = null;
                    }

                    lastTimestamp = data.TimestampMS;
                }
            }

            inputReader.Rewind();
            while ((data = inputReader.ReadFrame()) != null)
            {
                if (this.firstFrameBlack)
                {
                    if (startBlackTimestamp.HasValue && data.TimestampMS < startBlackTimestamp.Value)
                        continue;
                }
                else
                {
                    if (startBlackTimestamp.HasValue && data.TimestampMS <= startBlackTimestamp.Value)
                        continue;
                }

                if (endBlackTimestamp.HasValue && data.TimestampMS >= endBlackTimestamp.Value)
                    break;

                // See if the current data is the same as last
                if (currentData.Any())
                {
                    bool theSame = true;
                    foreach (var dmxDataFrame in data.DmxData)
                    {
                        if (currentData.TryGetValue(dmxDataFrame.UniverseId, out var lastData))
                        {
                            // Compare
                            if (!lastData.SequenceEqual(dmxDataFrame.Data))
                            {
                                theSame = false;
                                break;
                            }
                        }
                    }

                    if (theSame)
                        // Skip
                        continue;
                }

                if (!firstPosition.HasValue)
                {
                    firstPosition = data.Position;
                    firstTimestamp = data.TimestampMS;
                }

                foreach (var dmxDataFrame in data.DmxData)
                {
                    if (!firstData.ContainsKey(dmxDataFrame.UniverseId))
                    {
                        firstData.Add(dmxDataFrame.UniverseId, dmxDataFrame.Data);
                    }
                    else
                    {
                        currentData[dmxDataFrame.UniverseId] = dmxDataFrame.Data;

                        // Compare
                        int? diff = null;
                        int dataElements = 0;
                        foreach (var kvp in firstData)
                        {
                            byte[] compareData;
                            if (!currentData.TryGetValue(kvp.Key, out compareData))
                            {
                                diff = null;
                                break;
                            }

                            if (!diff.HasValue)
                                diff = 0;
                            int maxLen = Math.Min(kvp.Value.Length, compareData.Length);
                            for (int i = 0; i < maxLen; i++)
                                diff += Math.Abs(kvp.Value[i] - compareData[i]);
                            dataElements += maxLen;
                        }

                        if (diff.HasValue)
                            diffList.Add(((double)diff.Value / dataElements, data.Position, data.TimestampMS));
                    }
                }
            }

            if (startBlackTimestamp.HasValue)
                Console.WriteLine($"Start trimming at {startBlackTimestamp:N1} mS");

            diffList = diffList.OrderBy(x => x.Match).ThenByDescending(x => x.Position).ToList();

            foreach (var match in diffList.Take(10))
            {
                Console.WriteLine("Position: {0,6:P1}   Match: {1,7:P2}   Frame Count: {2,7:N0}   Duration: {3,12:N3} mS",
                    (double)match.Position / context.TotalFrames,
                    (100 - match.Match) / 100.0,
                    match.Position - firstPosition.Value,
                    match.TimestampMS - firstTimestamp);
            }

            if (diffList.Any())
            {
                var firstMatch = diffList.First();

                // Output command
                Console.WriteLine();
                Console.WriteLine("Trim command for first match:");
                if (startBlackTimestamp > 0)
                    Console.WriteLine($"PostProcessor -i {context.InputFilename} -c TrimTime -ts {startBlackTimestamp:F3} -td {firstMatch.TimestampMS - firstTimestamp:F3}");
                else
                    Console.WriteLine($"PostProcessor -i {context.InputFilename} -c TrimTime -td {firstMatch.TimestampMS - firstTimestamp:F3}");
            }
        }
    }
}
