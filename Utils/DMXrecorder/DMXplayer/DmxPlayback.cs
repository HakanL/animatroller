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
    public class DmxPlayback
    {
        private FileStream file;
        private BinaryReader binRead;
        private Stopwatch watch;
        private long nextStop;
        private DmxFrame dmxFrame;
        private CancellationTokenSource cts;
        private AcnStream acnStream;
        private Task runnerTask;

        public DmxPlayback(AcnStream acnStream)
        {
            this.acnStream = acnStream;
        }

        public void Run(bool loop)
        {
            this.watch = new Stopwatch();
            long timestampOffset = 0;

            this.cts = new CancellationTokenSource();

            this.acnStream.Start();

            this.runnerTask = Task.Run(() =>
            {
                do
                {
                    // See if we should restart
                    if (this.file.Position >= this.file.Length)
                    {
                        // Restart
                        this.file.Position = 0;
                        this.dmxFrame = null;
                        this.watch.Reset();
                    }

                    if (this.dmxFrame == null)
                    {
                        this.dmxFrame = ReadFrame(this.binRead);
                        timestampOffset = this.dmxFrame.TimestampMS;
                    }

                    this.watch.Start();

                    while (!this.cts.IsCancellationRequested && this.file.Position < this.file.Length)
                    {
                        // Calculate when the next stop is
                        this.nextStop = this.dmxFrame.TimestampMS - timestampOffset;

                        long msLeft = this.nextStop - this.watch.ElapsedMilliseconds;
                        if (msLeft <= 0)
                        {
                            // Output
                            OutputData(this.dmxFrame);

                            // Read next frame
                            this.dmxFrame = ReadFrame(this.binRead);
                            continue;
                        }
                        else if (msLeft < 16)
                        {
                            SpinWait.SpinUntil(() => this.watch.ElapsedMilliseconds >= this.nextStop);
                            continue;
                        }

                        Thread.Sleep(1);
                    }
                } while (!this.cts.IsCancellationRequested && loop);

                Console.WriteLine("Done...");

                this.watch.Stop();

                this.acnStream.Stop();

                Console.WriteLine("Done outputting");
            });
        }

        private void OutputData(DmxFrame dmxFrame)
        {
            if (dmxFrame.Data != null)
                this.acnStream.SendDmx(dmxFrame.Universe, dmxFrame.Data);
        }

        private DmxFrame ReadFrame(BinaryReader binRead)
        {
            var target = new DmxFrame();
            target.Start = binRead.ReadByte();
            target.TimestampMS = (uint)binRead.ReadInt32();
            target.Universe = (ushort)binRead.ReadInt16();
            switch (target.Start)
            {
                case 1:
                    target.Len = (ushort)binRead.ReadInt16();
                    target.Data = binRead.ReadBytes(target.Len);
                    break;

                case 2:
                    break;

                default:
                    throw new ArgumentException("Invalid data");
            }
            target.End = binRead.ReadByte();

            if (target.End != 4)
                throw new ArgumentException("Invalid data");

            return target;
        }

        public void Load(string fileName)
        {
            this.file = System.IO.File.OpenRead(fileName);
            this.binRead = new System.IO.BinaryReader(file);
        }

        public void Dispose()
        {
            this.cts.Cancel();

            this.runnerTask.Wait();

            if (this.file != null)
            {
                this.file.Dispose();

                this.file = null;
            }
        }
    }
}
