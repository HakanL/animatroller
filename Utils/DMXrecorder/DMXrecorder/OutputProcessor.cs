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
        private Common.IO.IFileWriter dataWriter;

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
                    this.dataWriter = new Common.IO.CsvFileWriter(args.OutputFile);
                    break;

                case Arguments.FileFormats.Binary:
                    this.dataWriter = new Common.IO.BinaryFileWriter(args.OutputFile);
                    break;

                case Arguments.FileFormats.PCapAcn:
                    this.dataWriter = new Common.IO.PCapAcnFileWriter(args.OutputFile);
                    break;

                case Arguments.FileFormats.PCapArtNet:
                    this.dataWriter = new Common.IO.PCapArtNetFileWriter(args.OutputFile);
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

                    while (this.receivedData.TryDequeue(out RawDmxData dmxData))
                    {
                        if (!this.universeData.TryGetValue(dmxData.Universe, out UniverseInfo previousData))
                        {
                            previousData = new UniverseInfo();
                            previousData.Data = new byte[dmxData.Data.Length];
                            this.universeData.Add(dmxData.Universe, previousData);
                        }

                        if (previousData.Data.Length != dmxData.Data.Length)
                            previousData.Data = new byte[dmxData.Data.Length];

                        Buffer.BlockCopy(dmxData.Data, 0, previousData.Data, 0, previousData.Data.Length);

                        //TODO: Destination
                        this.dataWriter.Output(Common.DmxDataOutputPacket.CreateFullFrame(
                            millisecond: dmxData.TimestampMS,
                            sequence: dmxData.Sequence,
                            universe: dmxData.Universe,
                            data: dmxData.Data,
                            syncAddress: dmxData.SyncAddress,
                            destination: null));

                        previousData.Sequence = dmxData.Sequence;
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

        public void CompleteUniverse(int universe)
        {
            this.dataWriter.Footer(universe);
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

            (this.dataWriter as IDisposable)?.Dispose();
            this.dataWriter = null;
        }
    }
}
