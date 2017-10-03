//#define DEBUG_LOG

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Ports;
using System.Reactive;
using System.Reactive.Subjects;
using Serilog;
using System.Net.Sockets;
using System.Net;
using System.IO;

namespace Animatroller.Framework.Expander
{
    public class Tpm2NetSink : IPort, IRunnable
    {
        const byte TPM2net_BlockStart = 0x9c;
        const byte TPM2_BlockStart = 0xc9;      // Not implemented yet
        const byte TPM2_BlockType_Data = 0xda;
        const byte TPM2_BlockType_Command = 0xc0;
        const byte TPM2_BlockType_Response = 0xaa;
        const byte TPM2_BlockEnd = 0x36;

        public enum BlockTypes
        {
            Data,
            Command,
            Response
        }

        protected ILogger log;
        private System.Threading.CancellationTokenSource cancelSource;
        protected Subject<Tpm2BlockData> dataReceived;

        public Tpm2NetSink([System.Runtime.CompilerServices.CallerMemberName] string name = "", int listenPort = 65506)
        {
            this.log = Log.Logger;
            dataReceived = new Subject<Tpm2BlockData>();

            this.cancelSource = new System.Threading.CancellationTokenSource();

            Task.Run(async () =>
            {
                using (var udpClient = new UdpClient(listenPort))
                {
                    var clientBuffer = new Dictionary<IPEndPoint, InternalBlockData>();

                    while (!this.cancelSource.IsCancellationRequested)
                    {
                        //IPEndPoint object will allow us to read datagrams sent from any source.
                        var receivedResults = await udpClient.ReceiveAsync();

                        InternalBlockData existingData;
                        clientBuffer.TryGetValue(receivedResults.RemoteEndPoint, out existingData);

                        if (existingData == null)
                        {
                            // New block
                            if (receivedResults.Buffer.Length < 5)
                                // Invalid
                                continue;

                            if (receivedResults.Buffer[0] != TPM2net_BlockStart)
                                // No start identifier
                                continue;

                            if (receivedResults.Buffer.Length < 7)
                                // Tpm2Net minimum size is 7 bytes (0 bytes of data, not sure if perhaps it should be minimum 8)
                                continue;

                            BlockTypes blockType;
                            switch (receivedResults.Buffer[1])
                            {
                                case TPM2_BlockType_Data:
                                    blockType = BlockTypes.Data;
                                    break;

                                case TPM2_BlockType_Command:
                                    blockType = BlockTypes.Command;
                                    break;

                                case TPM2_BlockType_Response:
                                    blockType = BlockTypes.Response;
                                    break;

                                default:
                                    // Unknown
                                    continue;
                            }

                            int blockSize = ((int)receivedResults.Buffer[2] << 8) + receivedResults.Buffer[3];

                            existingData = new InternalBlockData
                            {
                                Size = blockSize,
                                Type = blockType,
                                PacketNumber = receivedResults.Buffer[4],
                                TotalPackets = receivedResults.Buffer[5],
                                DataStream = new MemoryStream(blockSize)
                            };

                            int availableBytes = Math.Min(blockSize, receivedResults.Buffer.Length - 7);
                            existingData.DataStream.Write(receivedResults.Buffer, 6, availableBytes);
                        }
                        else
                        {
                            // Continue

                            int availableBytes = Math.Min(existingData.Size, receivedResults.Buffer.Length - 1);
                            existingData.DataStream.Write(receivedResults.Buffer, 0, availableBytes);
                        }

                        if (existingData.DataStream.Length == existingData.Size)
                        {
                            // Completed, check end of block
                            if (receivedResults.Buffer[receivedResults.Buffer.Length - 1] != TPM2_BlockEnd)
                            {
                                // Missing, throw away
                                existingData.Dispose();
                                if (clientBuffer.ContainsKey(receivedResults.RemoteEndPoint))
                                    clientBuffer.Remove(receivedResults.RemoteEndPoint);
                                continue;
                            }

                            // All good
#if DEBUG_LOG
                            log.Debug("Received block of {0} bytes", existingData.DataStream.Length);
#endif

                            this.dataReceived.OnNext(new Tpm2BlockData(receivedResults.RemoteEndPoint, existingData));

                            existingData.Dispose();
                        }
                        else
                        {
                            // Not received everything yet
                            if (!clientBuffer.ContainsKey(receivedResults.RemoteEndPoint))
                            {
                                clientBuffer.Add(receivedResults.RemoteEndPoint, existingData);
                            }
                        }
                    }

                    foreach (InternalBlockData blockData in clientBuffer.Values)
                        blockData.Dispose();
                    clientBuffer.Clear();
                }
            });

            Executor.Current.Register(this);
        }

        public IObservable<Tpm2BlockData> DataReceived
        {
            get { return this.dataReceived; }
        }

        public void Start()
        {
        }

        public void Stop()
        {
            this.cancelSource.Cancel();
        }

        public class InternalBlockData : IDisposable
        {
            public int Size { get; set; }

            public byte PacketNumber { get; set; }

            public byte TotalPackets { get; set; }

            public BlockTypes Type { get; set; }

            public MemoryStream DataStream { get; set; }

            public void Dispose()
            {
                DataStream.Dispose();
            }
        }

        public class Tpm2BlockData
        {
            public IPEndPoint EndPoint { get; private set; }

            public byte PacketNumber { get; private set; }

            public byte TotalPackets { get; private set; }

            public BlockTypes Type { get; private set; }

            public byte[] Data { get; private set; }

            public Tpm2BlockData(IPEndPoint endPoint, InternalBlockData blockData)
            {
                EndPoint = endPoint;
                PacketNumber = blockData.PacketNumber;
                TotalPackets = blockData.TotalPackets;
                Type = blockData.Type;
                Data = blockData.DataStream.ToArray();
            }
        }
    }
}
