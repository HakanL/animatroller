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
        private Common.IFileWriter dataWriter;

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
                    this.dataWriter = new Common.CsvFileWriter(args.OutputFile);
                    break;

                case Arguments.FileFormats.Binary:
                    this.dataWriter = new Common.BinaryFileWriter(args.OutputFile);
                    break;

                case Arguments.FileFormats.PCapAcn:
                    this.dataWriter = new Common.PCapAcnFileWriter(args.OutputFile);
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
                        bool newData = false;
                        UniverseInfo previousData;
                        if (!this.universeData.TryGetValue(dmxData.Universe, out previousData))
                        {
                            previousData = new UniverseInfo();
                            previousData.Data = new byte[dmxData.Data.Length];
                            this.universeData.Add(dmxData.Universe, previousData);
                            newData = true;
                        }

                        bool modified = !dmxData.Data.SequenceEqual(previousData.Data);
                        if (newData)
                            modified = true;

                        if (previousData.Data.Length != dmxData.Data.Length)
                            previousData.Data = new byte[dmxData.Data.Length];

                        Buffer.BlockCopy(dmxData.Data, 0, previousData.Data, 0, previousData.Data.Length);

                        if (modified)
                        {
                            this.dataWriter.Output(Common.DmxDataPacket.CreateFullFrame(
                                millisecond: dmxData.TimestampMS,
                                sequence: dmxData.Sequence,
                                universe: dmxData.Universe,
                                data: dmxData.Data,
                                syncAddress: dmxData.SyncAddress));
                        }
                        else
                        {
                            if (previousData.Sequence != dmxData.Sequence)
                            {
                                this.dataWriter.Output(Common.DmxDataPacket.CreateNoChange(
                                    millisecond: dmxData.TimestampMS,
                                    sequence: dmxData.Sequence,
                                    universe: dmxData.Universe));
                            }
                        }

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
