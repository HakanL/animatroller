using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.IO.Ports;
using System.Threading.Tasks;
using Animatroller.Framework.PhysicalDevice;

namespace Animatroller.Framework.Expander
{
    public class IOExpander : SerialController, IPort, IDmxOutput, IPixelOutput, IOutputHardware
    {
        private object lockObject = new object();
        private HashSet<byte> changedPixels;
        private byte[] pixelData;
        private Task senderTask;
        private System.Threading.CancellationTokenSource cancelSource;
        private System.Diagnostics.Stopwatch firstChange;
        private int sentUpdates;
        private int receivedUpdates;

        public IOExpander(string portName) : base(portName, 0)
        {
            this.DigitalInputs = new PhysicalDevice.DigitalInput[3];
            for(int index = 0; index < this.DigitalInputs.Length; index++)
                this.DigitalInputs[index] = new PhysicalDevice.DigitalInput();

            this.DigitalOutputs = new PhysicalDevice.DigitalOutput[4];
            for (int index = 0; index < this.DigitalOutputs.Length; index++)
                WireupOutput(index);

            this.Motor = new PhysicalDevice.MotorWithFeedback((target, speed, timeout) =>
                {
                    SendSerialCommand(string.Format("M,{0},{1},{2:F0},{3:F0}", 1, target, speed * 100, timeout.TotalSeconds));
                });

            this.changedPixels = new HashSet<byte>();
            this.cancelSource = new System.Threading.CancellationTokenSource();
            this.firstChange = new System.Diagnostics.Stopwatch();
            this.pixelData = new byte[4 * 50];
            for (int i = 4; i < this.pixelData.Length; i += 4)
                this.pixelData[i] = 32;

            this.senderTask = new Task(x =>
            {
                byte[] bytesToSend = null;

                while (!this.cancelSource.IsCancellationRequested)
                {
                    lock (lockObject)
                    {
                        if (this.changedPixels.Any())
                        {
                            this.firstChange.Stop();
                            this.sentUpdates++;
                            log.Info("Sending {0} changes to IOExpander. Oldest {1:N2}ms. Recv: {2}   Sent: {3}",
                                this.changedPixels.Count, this.firstChange.Elapsed.TotalMilliseconds,
                                receivedUpdates, sentUpdates);

                            if (this.changedPixels.Count <= 2)
                            {
                                foreach (var channel in this.changedPixels)
                                {
                                    int dataOffset = 1 + channel * 4;

                                    var shortSend = new byte[] { (byte)channel,
                                        this.pixelData[dataOffset + 0],
                                        this.pixelData[dataOffset + 1],
                                        this.pixelData[dataOffset + 2]};

                                    SendSerialCommand((byte)'R', shortSend);
                                }
                            }
                            else
                            {
                                // Send everything
                                bytesToSend = new byte[this.pixelData.Length];
                                Array.Copy(this.pixelData, bytesToSend, this.pixelData.Length);
                            }
                            this.changedPixels.Clear();
                        }
                    }

                    if (bytesToSend != null)
                    {
                        // Takes about 40 ms to send the data for all pixels
                        SendSerialCommand((byte)'R', bytesToSend);
                        bytesToSend = null;
                    } else
                        System.Threading.Thread.Sleep(10);
                }
            }, this.cancelSource.Token, TaskCreationOptions.LongRunning);

            this.senderTask.Start();

            Executor.Current.Register(this);
        }

        public PhysicalDevice.DigitalInput[] DigitalInputs { get; private set; }
        public PhysicalDevice.DigitalOutput[] DigitalOutputs { get; private set; }
        public PhysicalDevice.MotorWithFeedback Motor { get; private set; }

        public override void Start()
        {
            base.Start();
        }

        public override void Stop()
        {
            this.cancelSource.Cancel();
            base.Stop();
        }

        private void WireupOutput(int index)
        {
            this.DigitalOutputs[index] = new PhysicalDevice.DigitalOutput(x =>
            {
                SendSerialCommand(string.Format("O,{0},{1}", index + 1, x ? 1 : 0));
            });
        }

        protected override void CommandReceived(string data)
        {
            var cmd = data[0];
            data = data.Substring(1).TrimStart(',');
            string[] parts = data.Split(',');

            switch (cmd)
            {
                case '#':
                    log.Info("ACK " + data);
                    break;
                case 'X':
                    log.Info("Init " + data);
                    break;
                case 'I':
                    // Input
                    if (parts.Length >= 2)
                    {
                        byte chn = byte.Parse(parts[0]);
                        byte val = byte.Parse(parts[1]);

                        log.Info("Input chn={0} val={1}", chn, val);
                        switch (chn)
                        {
                            case 1:
                                this.DigitalInputs[0].Trigger((val & 1) == 1);
                                break;
                            case 2:
                                this.DigitalInputs[1].Trigger((val & 1) == 1);
                                break;
                            case 3:
                                this.DigitalInputs[2].Trigger((val & 1) == 1);
                                break;
                        }
                    }
                    break;
                case 'M':
                    // Motor Controller
                    if (parts.Length >= 2)
                    {
                        byte chn = byte.Parse(parts[0]);
                        int? pos;
                        if (parts[1] != "X")
                        {
                            if (parts[1].StartsWith("S"))
                            {
                                pos = int.Parse(parts[1].Substring(1));
                                log.Info("MotorController chn={0} Starting at {1}", chn, pos);
                            }
                            else if (parts[1].StartsWith("E"))
                            {
                                pos = int.Parse(parts[1].Substring(1));
                                log.Info("MotorController chn={0} Ends at {1}", chn, pos);

                                if (chn == 1)
                                    this.Motor.Trigger(pos, pos == null);
                            }
                            else
                            {
                                pos = int.Parse(parts[1]);
                                log.Info("MotorController chn={0} val={1}", chn, pos);
                            }
                        }
                        else
                        {
                            // Motor failed
                            pos = null;
                            log.Info("MotorController chn={0} failed", chn);

                            if (chn == 1)
                                this.Motor.Trigger(pos, pos == null);
                        }

                    }
                    break;
                default:
                    log.Info("Unknown data: " + data);
                    break;
            }
        }

        public SendStatus SendDimmerValue(int channel, byte value)
        {
            SendSerialCommand(string.Format("L,{0},{1}", channel, value));

            return SendStatus.NotSet;
        }

        public SendStatus SendDimmerValues(int firstChannel, byte[] values)
        {
            return SendDimmerValues(firstChannel, values, 0, values.Length);
        }

        public SendStatus SendDimmerValues(int firstChannel, byte[] values, int offset, int length)
        {
            for (int i = 0; i < length; i++)
            {
                SendSerialCommand(string.Format("L,{0},{1}", firstChannel + i, values[offset + i]));
            }

            return SendStatus.NotSet;
        }

        public IOExpander Connect(PhysicalDevice.INeedsDmxOutput device)
        {
            device.DmxOutputPort = this;

            return this;
        }

        public IOExpander Connect(PhysicalDevice.INeedsPixelOutput device)
        {
            device.PixelOutputPort = this;

            return this;
        }

        public SendStatus SendPixelValue(int channel, PhysicalDevice.PixelRGBByte rgb)
        {
            int dataOffset = 1 + channel * 4;
            lock (lockObject)
            {
                if (!this.changedPixels.Any())
                    this.firstChange.Restart();

                this.pixelData[dataOffset + 0] = rgb.R;
                this.pixelData[dataOffset + 1] = rgb.G;
                this.pixelData[dataOffset + 2] = rgb.B;

                this.changedPixels.Add((byte)channel);
                receivedUpdates++;
            }

            return SendStatus.NotSet;
        }

        public SendStatus SendPixelsValue(int startChannel, PhysicalDevice.PixelRGBByte[] rgb)
        {
            lock (lockObject)
            {
                if (!this.changedPixels.Any())
                    this.firstChange.Restart();

                int readOffset = 0;
                for (int i = 0; i < rgb.Length; i++)
                {
                    int dataOffset = 1 + (startChannel + i) * 4;

                    this.pixelData[dataOffset + 0] = rgb[readOffset].R;
                    this.pixelData[dataOffset + 1] = rgb[readOffset].G;
                    this.pixelData[dataOffset + 2] = rgb[readOffset].B;
                    readOffset++;

                    this.changedPixels.Add((byte)(startChannel + i));
                }
                receivedUpdates++;
            }

            return SendStatus.NotSet;
        }

        public void SendPixelsValue(int channel, byte[] rgb, int length)
        {
            throw new NotImplementedException();
        }
    }
}
