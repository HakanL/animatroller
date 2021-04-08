using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Animatroller.DMXplayer
{
    public class DmxPlayback : IDisposable
    {
        private readonly Common.IInputReader fileReader;
        private CancellationTokenSource cts;
        private Task runnerTask;
        private Dictionary<int, HashSet<int>> universeMapping;
        private readonly Dictionary<int, double> lastFrameTimestampPerUniverse = new Dictionary<int, double>();
        private readonly Dictionary<int, double> intervalPerUniverse = new Dictionary<int, double>();
        private readonly Scheduler scheduler;

        public DmxPlayback(Common.IInputReader fileReader, IOutput output, int periodMS, int sendSyncUniverseId)
        {
            this.fileReader = fileReader;
            this.scheduler = new Scheduler(output, periodMS, sendSyncUniverseId: sendSyncUniverseId);

            this.scheduler.ProgressFrames.Subscribe(info =>
            {
                Console.WriteLine($"{info.TimestampMS:N2} ms - played {info.PlayedFrames} frames");
            });
        }

        public void WaitForCompletion()
        {
            this.runnerTask.Wait();
        }

        public void Cancel()
        {
            this.cts.Cancel();
        }

        public IDictionary<int, HashSet<int>> UniverseMapping => this.universeMapping;

        public void AddUniverseMapping(int inputUniverse, int outputUniverse)
        {
            if (this.universeMapping == null)
                this.universeMapping = new Dictionary<int, HashSet<int>>();

            if (!this.universeMapping.TryGetValue(inputUniverse, out var outputList))
            {
                outputList = new HashSet<int>();
                this.universeMapping.Add(inputUniverse, outputList);
            }

            outputList.Add(outputUniverse);
        }

        public void Run(int loop)
        {
            double timestampOffset = 0;

            this.cts = new CancellationTokenSource();

            var waitHandles = new WaitHandle[] { this.cts.Token.WaitHandle, this.scheduler.FillBuffer };

            int loopCount = 1;
            Scheduler.SyncModes syncMode = Scheduler.SyncModes.Timestamp;

            this.runnerTask = Task.Run(() =>
            {
                Common.DmxDataPacket dmxFrame = null;

                do
                {
                    if (dmxFrame == null)
                    {
                        Console.WriteLine($"Reading first frame, iteration {loopCount}");

                        dmxFrame = this.fileReader.ReadFrame();
                        if (dmxFrame == null)
                            break;

                        var dmxDataFrame = dmxFrame.Content as Common.DmxDataFrame;
                        if (dmxDataFrame != null && dmxDataFrame.UniverseId.HasValue)
                        {
                            this.lastFrameTimestampPerUniverse.TryGetValue(dmxDataFrame.UniverseId.Value, out double lastFrameTimestamp);

                            timestampOffset += lastFrameTimestamp;

                            this.intervalPerUniverse.TryGetValue(dmxDataFrame.UniverseId.Value, out double interval);
                            this.lastFrameTimestampPerUniverse.Clear();
                            timestampOffset += interval;
                        }
                    }

                    while (!this.cts.IsCancellationRequested)
                    {
                        if (dmxFrame.Content is Common.SyncFrame)
                        {
                            syncMode = Scheduler.SyncModes.Manual;
                            this.scheduler.SendCurrentData();
                        }
                        else
                        {
                            var dmxDataFrame = dmxFrame.Content as Common.DmxDataFrame;

                            this.lastFrameTimestampPerUniverse.TryGetValue(dmxDataFrame.UniverseId.Value, out double lastFrameTimestamp);
                            this.lastFrameTimestampPerUniverse[dmxDataFrame.UniverseId.Value] = dmxFrame.TimestampMS;
                            double interval = dmxFrame.TimestampMS - lastFrameTimestamp;
                            if (interval > 0)
                            {
                                this.intervalPerUniverse[dmxDataFrame.UniverseId.Value] = interval;
                            }
                            else
                            {
                                // Default
                                this.intervalPerUniverse[dmxDataFrame.UniverseId.Value] = this.scheduler.PeriodMS;
                            }

                            if (this.universeMapping != null)
                            {
                                if (this.universeMapping.TryGetValue(dmxDataFrame.UniverseId.Value, out var outputUniverses))
                                {
                                    foreach (int outputUniverse in outputUniverses)
                                    {
                                        this.scheduler.AddData(dmxFrame.TimestampMS + timestampOffset, outputUniverse, dmxDataFrame.Data, syncMode);
                                    }
                                }
                            }
                            else
                            {
                                // No mapping
                                this.scheduler.AddData(dmxFrame.TimestampMS + timestampOffset, dmxDataFrame.UniverseId.Value, dmxDataFrame.Data, syncMode);
                            }
                        }

                        WaitHandle.WaitAny(waitHandles);

                        if (!this.scheduler.IsClockRunning && this.scheduler.IsQueueFilled)
                        {
                            // Start
                            this.scheduler.StartOutput();
                        }

                        // Read next frame
                        dmxFrame = this.fileReader.ReadFrame();

                        if (dmxFrame == null)
                            // End of file
                            break;
                    }

                    loopCount++;

                    if (dmxFrame == null)
                    {
                        // Restart
                        this.fileReader.Rewind();
                    }

                } while (!this.cts.IsCancellationRequested && (loop < 0 || loopCount <= loop));

                this.scheduler.EndOfData();

                // If we have an extremely short sequence then we need to start the clock here
                if (!this.scheduler.IsClockRunning)
                    this.scheduler.StartOutput();

                WaitHandle.WaitAny(new WaitHandle[] { this.cts.Token.WaitHandle, this.scheduler.QueueEmpty });

                Console.WriteLine($"Playback completed, {this.scheduler.PlayedFrames} frames played, {this.fileReader.FramesRead} frames read");
            });
        }

        public void Dispose()
        {
            this.cts.Cancel();

            this.runnerTask.Wait();

            this.scheduler.Dispose();
        }
    }
}
