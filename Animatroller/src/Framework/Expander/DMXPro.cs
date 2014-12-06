using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Ports;
using NLog;

namespace Animatroller.Framework.Expander
{
    public class DMXPro : IPort, IRunnable, IDmxOutput, IOutputHardware
    {
        protected static Logger log = LogManager.GetCurrentClassLogger();
        private int sendCounter;
        private SerialPort serialPort;
        private object lockObject = new object();
        private DmxPacketManager packetManager;
        private bool foundDmxPro;
        private System.Threading.ManualResetEvent initWait;
        private byte[] dmxData;
        private int dataChanges;
        private Task senderTask;
        private System.Threading.CancellationTokenSource cancelSource;
        private System.Diagnostics.Stopwatch firstChange;

        public DMXPro(string portName)
        {
            this.initWait = new System.Threading.ManualResetEvent(false);
            this.serialPort = new SerialPort(portName, 38400);
            this.serialPort.DataReceived += serialPort_DataReceived;
            this.packetManager = new DmxPacketManager();
            this.packetManager.PacketReceived += packetManager_PacketReceived;
            this.dmxData = new byte[513];
            // Set Start Code
            this.dmxData[0] = 0;

            this.cancelSource = new System.Threading.CancellationTokenSource();
            this.firstChange = new System.Diagnostics.Stopwatch();

            this.senderTask = new Task(x =>
                {
                    while (!this.cancelSource.IsCancellationRequested)
                    {
                        bool sentChanges = false;

                        lock (lockObject)
                        {
                            if (this.dataChanges > 0)
                            {
                                this.firstChange.Stop();
                                //log.Info("Sending {0} changes to DMX Pro. Oldest {1:N2}ms",
                                //    this.dataChanges, this.firstChange.Elapsed.TotalMilliseconds);
                                this.dataChanges = 0;
                                sentChanges = true;

                                SendSerialCommand(6, this.dmxData);
                            }
                        }

                        if(!sentChanges)
                            System.Threading.Thread.Sleep(10);
                    }
                }, this.cancelSource.Token, TaskCreationOptions.LongRunning);

            Executor.Current.Register(this);
        }

        protected void packetManager_PacketReceived(object sender, DmxPacketManager.DmxPacketReceivedEventArgs e)
        {
            string bufData = string.Join(",", e.PacketData.ToList().ConvertAll(x => x.ToString("d")));
            log.Info("Received from DMXPro: Label: {0:d}   Payload: {1}", e.Label, bufData);

            if (!foundDmxPro)
            {
                // Check if we have a DMX Pro
                if (e.PacketData.Length == 5)
                {
                    int version = e.PacketData[0] + (e.PacketData[1] << 8);
                    if (version >= 300)
                        foundDmxPro = true;
                    this.initWait.Set();
                }
            }
        }

        private void serialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                var sp = (SerialPort)sender;

                if (sp.BytesToRead == 0)
                    return;

                var buf = new byte[sp.BytesToRead];
                sp.Read(buf, 0, buf.Length);

#if DEBUG
                string bufData = string.Join(",", buf.ToList().ConvertAll(x => x.ToString("d")));
                log.Info("Received from Serial Port: {0}", bufData);
#endif

                this.packetManager.WriteNewData(buf);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Exception in serialPort_DataReceived: {0}", ex.Message);
            }
        }

        protected void SendSerialCommand(byte label, byte[] data)
        {
            if (data.Length > 600)
                throw new ArgumentOutOfRangeException("Max data size is 600 bytes");

            lock (lockObject)
            {
                sendCounter++;
//                log.Info("Sending packet {0} to DMX", sendCounter);

                try
                {
                    var header = new byte[] { 0x7E, label, (byte)(data.Length & 0xFF), (byte)(data.Length >> 8) };
                    var footer = new byte[] { 0xE7 };
                    serialPort.Write(header, 0, header.Length);
                    if (data.Length > 0)
                        serialPort.Write(data, 0, data.Length);
                    serialPort.Write(footer, 0, footer.Length);
                }
                catch (Exception ex)
                {
                    log.Info("SendSerialCommand exception: " + ex.Message);
                    // Ignore
                }
            }
        }

        private void DataChanged()
        {
            lock (lockObject)
            {
                if (this.dataChanges++ == 0)
                {
                    this.firstChange.Restart();
                }
            }
        }

        public SendStatus SendDimmerValue(int channel, byte value)
        {
            return SendDimmerValues(channel, new byte[] { value }, 0, 1);
        }

        public SendStatus SendDimmerValues(int firstChannel, byte[] values)
        {
            return SendDimmerValues(firstChannel, values, 0, values.Length);
        }

        public SendStatus SendDimmerValues(int firstChannel, byte[] values, int offset, int length)
        {
            if (!foundDmxPro)
                throw new ArgumentException("No DMX Pro found");

            if (firstChannel < 1 || firstChannel + values.Length - 1 > 512)
                throw new ArgumentOutOfRangeException("Invalid first channel (1-512)");

            for(int i = 0; i < length; i++)
                this.dmxData[firstChannel + i] = values[offset + i];

            DataChanged();

            return SendStatus.NotSet;
        }

        public void Start()
        {
            serialPort.Open();

            SendSerialCommand(3, new byte[] { 0, 0 });
            if (!initWait.WaitOne(1000))
                throw new InvalidOperationException("No DMX Pro found on port " + this.serialPort.PortName);
            if (!foundDmxPro)
                throw new InvalidOperationException("Invalid DMX Pro (version) found");

            this.senderTask.Start();
        }

        public void Run()
        {
        }

        public void Stop()
        {
            this.cancelSource.Cancel();
            serialPort.Close();
        }

        public DMXPro Connect(PhysicalDevice.INeedsDmxOutput device)
        {
            device.DmxOutputPort = this;

            return this;
        }
    }

    public class DmxPacketManager : PacketManager
    {
        private byte? label;

        public class DmxPacketReceivedEventArgs : EventArgs
        {
            public byte Label { get; private set; }
            public byte[] PacketData { get; private set; }
            public DmxPacketReceivedEventArgs(byte label, byte[] packetData)
            {
                this.Label = label;
                this.PacketData = packetData;
            }
        }

        public new event EventHandler<DmxPacketReceivedEventArgs> PacketReceived;

        public DmxPacketManager()
            : base(new byte[] { 0x7E }, new byte[] { 0xE7 })
        {
            base.PacketReceived += DmxPacketManager_PacketReceived;
        }

        protected void DmxPacketManager_PacketReceived(object sender, PacketManager.PacketReceivedEventArgs e)
        {
            // Payload is ContentSize - 3
            var payload = new byte[e.ContentSize - 3];
            Buffer.BlockCopy(e.Buffer, 3, payload, 0, payload.Length);

            var handler = PacketReceived;
            if (handler != null)
                handler(this, new DmxPacketReceivedEventArgs(this.label.Value, payload));
        }

        protected override int? GetContentSize(byte[] buf, int size)
        {
            // 0 = Start of message (0x7E) (already checked when we get here)
            // 1 = Label
            // 2 = Length LSB
            // 3 = Length MSB
            // 3+length = Payload
            // n = End of message (0xE7)

            // Need at least 3 bytes 
            if (size < 3)
            {
                return null;
            }

            this.label = buf[0];
            return buf[1] + (buf[2] << 8) + 3;    // 3 for label + length LSB/MSB
        }

        protected override void Reset()
        {
            base.Reset();
            this.label = null;
        }
    }
}
