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
    public class OutputProcessor : IDisposable
    {
        protected class UniverseInfo
        {
            public byte[] Data { get; set; }

            public long Sequence { get; set; }
        }

        private ConcurrentQueue<RawDmxData> receivedData;
        private ManualResetEvent fileTrigger;
        private int samplesReceived;
        private int samplesWritten;
        private bool isRunning;
        private Dictionary<int, UniverseInfo> universeData;
        private FileWriter dataWriter;

        public OutputProcessor(Arguments args)
        {
            if (args.Universes.Length == 0)
                throw new ArgumentException("No universes specified");

            this.receivedData = new ConcurrentQueue<RawDmxData>();

            this.fileTrigger = new ManualResetEvent(false);
            this.universeData = new Dictionary<int, UniverseInfo>();

            switch (args.FileFormat)
            {
                case Arguments.FileFormats.Csv:
                    this.dataWriter = new CsvFileWriter(args.OutputFile);
                    break;

                case Arguments.FileFormats.Binary:
                    this.dataWriter = new BinaryFileWriter(args.OutputFile);
                    break;

                default:
                    throw new ArgumentException("Invalid File Format");
            }

            this.isRunning = true;

            Task.Run(() =>
            {
                while (isRunning)
                {
                    this.fileTrigger.WaitOne();
                    this.fileTrigger.Reset();

                    RawDmxData dmxData;
                    while (this.receivedData.TryDequeue(out dmxData))
                    {
                        UniverseInfo previousData;
                        if (!this.universeData.TryGetValue(dmxData.Universe, out previousData))
                        {
                            previousData = new UniverseInfo();
                            previousData.Data = new byte[dmxData.Data.Length];
                            this.universeData.Add(dmxData.Universe, previousData);
                        }

                        bool modified = !dmxData.Data.SequenceEqual(previousData.Data);

                        if (previousData.Data.Length != dmxData.Data.Length)
                            previousData.Data = new byte[dmxData.Data.Length];

                        Buffer.BlockCopy(dmxData.Data, 0, previousData.Data, 0, previousData.Data.Length);
                        previousData.Sequence = dmxData.Sequence;

                        if (modified)
                        {
                            this.dataWriter.Output(OutputDmxData.CreateFullFrame(
                                millisecond: dmxData.Timestamp,
                                sequence: dmxData.Sequence,
                                universe: dmxData.Universe,
                                data: dmxData.Data));
                        }
                        else
                        {
                            if (previousData.Sequence != dmxData.Sequence)
                                this.dataWriter.Output(OutputDmxData.CreateNoChange(
                                    millisecond: dmxData.Timestamp,
                                    sequence: dmxData.Sequence,
                                    universe: dmxData.Universe));
                        }

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

        public void AddData(RawDmxData dmxData)
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
