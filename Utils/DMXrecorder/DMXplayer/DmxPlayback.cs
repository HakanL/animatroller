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
        private Common.IFileReader fileReader;
        private Stopwatch masterClock;
        private double nextStop;
        private CancellationTokenSource cts;
        private IOutput output;
        private Task runnerTask;
        private Dictionary<int, HashSet<int>> universeMapping;
        private Dictionary<int, double> lastFrameTimestampPerUniverse = new Dictionary<int, double>();
        private Dictionary<int, double> intervalPerUniverse = new Dictionary<int, double>();

        public DmxPlayback(Common.IFileReader fileReader, IOutput output)
        {
            this.fileReader = fileReader;
            this.output = output;
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
            this.masterClock = new Stopwatch();
            double timestampOffset = 0;

            this.cts = new CancellationTokenSource();

            int loopCount = 0;

            this.runnerTask = Task.Run(() =>
            {
                Common.DmxData dmxFrame = null;

                do
                {
                    int frames = 0;
                    var watch = Stopwatch.StartNew();

                    // See if we should restart
                    if (!this.fileReader.DataAvailable)
                    {
                        // Restart
                        this.fileReader.Rewind();
                        dmxFrame = null;
                        this.masterClock.Reset();
                    }

                    if (dmxFrame == null)
                    {
                        dmxFrame = this.fileReader.ReadFrame();
                        timestampOffset = dmxFrame.TimestampMS;
                        this.intervalPerUniverse.TryGetValue(dmxFrame.UniverseId, out double interval);
                        this.lastFrameTimestampPerUniverse.Clear();
                        timestampOffset -= interval;
                    }

                    this.masterClock.Start();

                    while (!this.cts.IsCancellationRequested)
                    {
                        this.lastFrameTimestampPerUniverse.TryGetValue(dmxFrame.UniverseId, out double lastFrameTimestamp);
                        this.lastFrameTimestampPerUniverse[dmxFrame.UniverseId] = dmxFrame.TimestampMS;
                        double interval = dmxFrame.TimestampMS - lastFrameTimestamp;
                        if (interval > 0)
                            this.intervalPerUniverse[dmxFrame.UniverseId] = dmxFrame.TimestampMS - lastFrameTimestamp;

                        // Calculate when the next stop is
                        this.nextStop = dmxFrame.TimestampMS - timestampOffset;

                        double msLeft = this.nextStop - this.masterClock.Elapsed.TotalMilliseconds;
                        if (msLeft <= 0)
                        {
                            // Output
                            if (dmxFrame.DataType == Common.DmxData.DataTypes.FullFrame && dmxFrame.Data != null)
                            {
                                if (this.universeMapping != null)
                                {
                                    if (this.universeMapping.TryGetValue(dmxFrame.UniverseId, out var outputUniverses))
                                    {
                                        foreach (int outputUniverse in outputUniverses)
                                        {
                                            this.output.SendDmx(outputUniverse, dmxFrame.Data);
                                        }
                                    }
                                }
                                else
                                {
                                    // No mapping
                                    this.output.SendDmx(dmxFrame.UniverseId, dmxFrame.Data);
                                }
                            }

                            frames++;

                            if (frames % 100 == 0)
                                Console.WriteLine("{0} Played back {1} frames", this.masterClock.Elapsed.ToString(@"hh\:mm\:ss\.fff"), frames);

                            if (!this.fileReader.DataAvailable)
                                break;

                            // Read next frame
                            dmxFrame = this.fileReader.ReadFrame();
                            continue;
                        }
                        else if (msLeft < 16)
                        {
                            SpinWait.SpinUntil(() => this.masterClock.ElapsedMilliseconds >= (long)this.nextStop);
                            continue;
                        }

                        Thread.Sleep(1);
                    }

                    loopCount++;
                    watch.Stop();

                    Console.WriteLine("Playback complete {0:N1} s, {1} frames, iteration {2}", watch.Elapsed.TotalSeconds, frames, loopCount);

                } while (!this.cts.IsCancellationRequested && (loop < 0 || loopCount <= loop));

                this.masterClock.Stop();

                Console.WriteLine("Playback completed");
            });
        }

        public void Dispose()
        {
            this.cts.Cancel();

            this.runnerTask.Wait();
        }
    }
}
