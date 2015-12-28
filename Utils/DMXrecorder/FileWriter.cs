using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Animatroller.DMXrecorder
{
    public class FileWriter : IDisposable
    {
        private ConcurrentQueue<DmxData> receivedData;
        private ManualResetEvent fileTrigger;
        private int samplesReceived;
        private int samplesWritten;
        private bool isRunning;
        private Dictionary<int, UniverseData> universes;
        private DataWriter dataWriter;

        public FileWriter(Arguments args)
        {
            if (args.Universes.Length == 0)
                throw new ArgumentException("No universes specified");

            this.receivedData = new ConcurrentQueue<DmxData>();

            this.fileTrigger = new ManualResetEvent(false);

            this.dataWriter = new DataWriter(args.OutputFile, args.FileFormat);

            this.isRunning = true;

            Task.Run(() =>
            {
                while (isRunning)
                {
                    this.fileTrigger.WaitOne();
                    this.fileTrigger.Reset();

                    DmxData dmxData;
                    while (this.receivedData.TryDequeue(out dmxData))
                    {
                        this.dataWriter.Output(dmxData);

                        this.samplesWritten++;
                    }

                    if ((this.samplesReceived % 100) == 0)
                        Console.WriteLine("Received {0}, written {1} samples", this.samplesReceived, this.samplesWritten);
                }
            });
        }

        public void AddUniverse(int universe)
        {
            this.dataWriter.Header(universe);
        }

        public void AddData(DmxData dmxData)
        {
            samplesReceived++;

            this.receivedData.Enqueue(dmxData);

            this.fileTrigger.Set();
        }

        public void Dispose()
        {
            this.isRunning = false;
            // Trigger
            this.fileTrigger.Set();

            Console.WriteLine("Received {0} samples", this.samplesReceived);

            this.dataWriter.Dispose();
            this.dataWriter = null;
        }
    }
}
