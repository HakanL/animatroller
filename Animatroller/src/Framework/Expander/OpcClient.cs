using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.IO.Ports;
using System.Reactive;
using System.Reactive.Subjects;
using System.Net;
using NLog;
using System.Net.Sockets;
using System.Reactive.Concurrency;
using System.Reactive.Linq;

namespace Animatroller.Framework.Expander
{
    /// <summary>
    /// Open Pixel Control client
    /// </summary>
    public class OpcClient : IPort, IRunnable
    {
        protected class OpcPixelUniverse : IPixelOutput
        {
            private OpcClient opcClient;
            private byte opcChannel;

            public OpcPixelUniverse(OpcClient opcClient, int opcChannel)
            {
                this.opcClient = opcClient;
                this.opcChannel = (byte)opcChannel;
            }

            public SendStatus SendPixelValue(int channel, PhysicalDevice.PixelRGBByte rgb)
            {
                throw new NotImplementedException();
            }

            public SendStatus SendPixelsValue(int channel, PhysicalDevice.PixelRGBByte[] rgb)
            {
                byte[] rgbArray = new byte[rgb.Length * 3];

                int bytePos = 0;
                for (int i = 0; i < rgb.Length; i++)
                {
                    rgbArray[bytePos++] = rgb[i].R;
                    rgbArray[bytePos++] = rgb[i].G;
                    rgbArray[bytePos++] = rgb[i].B;
                }

                this.opcClient.Send(this.opcChannel, (byte)0, rgbArray);

                return SendStatus.NotSet;
            }
        }

        const int OPC_DEFAULT_PORT = 7890;

        protected static Logger log = LogManager.GetCurrentClassLogger();
        private Socket socket;
        private byte[] sendData;
        private object lockObject = new object();
        private object socketLock = new object();
        private CancellationTokenSource cancelSource;
        private ManualResetEvent readyToSend = new ManualResetEvent(false);

        public OpcClient(string destination, int destinationPort = OPC_DEFAULT_PORT)
        {
            var destinationAddress = IPAddress.Parse(destination);

            this.cancelSource = new CancellationTokenSource();

            var task = Task.Factory.StartNew(() =>
                {
                    while (!this.cancelSource.IsCancellationRequested)
                    {
                        readyToSend.WaitOne();

                        readyToSend.Reset();

                        byte[] data;
                        lock (this.lockObject)
                        {
                            data = this.sendData.ToArray();
                        }

                        try
                        {
                            lock (this.socketLock)
                            {
                                // Create socket if we need one
                                if (this.socket == null)
                                {
                                    this.socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
                                    this.socket.NoDelay = true;
                                    this.socket.Connect(destinationAddress, destinationPort);
                                }
                            }

                            if (data.Length > 0)
                                this.socket.Send(data);
                        }
                        catch (Exception ex)
                        {
                            log.Warn(ex, "Failed to send OPC data");

                            if (this.socket != null && this.socket.Connected)
                                this.socket.Close();

                            this.socket = null;
                        }
                    }

                    if (this.socket != null)
                    {
                        try
                        {
                            this.socket.Close();
                            this.socket.Dispose();
                        }
                        catch (Exception ex)
                        {
                            log.Warn(ex, "Failed to close socket");
                        }

                        this.socket = null;
                    }
                }, cancelSource.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);

            Executor.Current.Register(this);
        }

        public void Start()
        {
        }

        public void Stop()
        {
            this.cancelSource.Cancel();
        }

        internal void Send(byte channel, byte command, byte[] data)
        {
            byte[] fullData = new byte[4 + data.Length];

            fullData[0] = channel;
            fullData[1] = command;
            fullData[2] = (byte)(data.Length >> 8);
            fullData[3] = (byte)data.Length;

            Buffer.BlockCopy(data, 0, fullData, 4, data.Length);

            lock (lockObject)
            {
                this.sendData = fullData;
                this.readyToSend.Set();
            }
        }

        public void Connect(PhysicalDevice.INeedsPixelOutput device, int opcChannel)
        {
            device.PixelOutputPort = new OpcPixelUniverse(this, opcChannel);
        }

        public void Connect(LogicalDevice.IPixel2D device, LogicalDevice.PixelMapper2D pixelMapper, int opcChannel)
        {
            device.Output.Subscribe(x =>
                {
                    Send((byte)opcChannel, 0, pixelMapper.GetByteArray(x));
                });
        }
    }
}
