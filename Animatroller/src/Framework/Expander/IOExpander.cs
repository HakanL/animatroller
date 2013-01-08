﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.IO.Ports;
using System.Threading.Tasks;
using Animatroller.Framework.PhysicalDevice;

namespace Animatroller.Framework.Expander
{
    public class IOExpander : SerialController, IPort, IDmxOutput, IPixelOutput
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
                            Console.WriteLine("Sending {0} changes to IOExpander. Oldest {1:N2}ms. Recv: {2}   Sent: {3}",
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
                    Console.WriteLine("ACK " + data);
                    break;
                case 'I':
                    // Input
                    if (parts.Length >= 2)
                    {
                        byte chn = byte.Parse(parts[0]);
                        byte val = byte.Parse(parts[1]);

                        Console.WriteLine("Input chn={0} val={1}", chn, val);
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
                            pos = int.Parse(parts[1]);
                        else
                            // Motor failed
                            pos = null;

                        Console.WriteLine("MotorController chn={0} val={1}", chn, pos);
                        if(chn == 1)
                            this.Motor.Trigger(pos, pos == null);
                    }
                    break;
                default:
                    Console.WriteLine("Unknown data: " + data);
                    break;
            }
        }

        public SendStatus SendDimmerValue(int channel, byte value)
        {
            SendSerialCommand(string.Format("L,{0},{1}", channel, value));

            return SendStatus.NotSet;
        }

        public SendStatus SendDimmerValues(int firstChannel, params byte[] values)
        {
            for (int i = 0; i < values.Length; i++)
            {
                SendSerialCommand(string.Format("L,{0},{1}", firstChannel + i, values[i]));
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

        public SendStatus SendPixelValue(int channel, byte r, byte g, byte b)
        {
            int dataOffset = 1 + channel * 4;
            lock (lockObject)
            {
                if (!this.changedPixels.Any())
                    this.firstChange.Restart();

                this.pixelData[dataOffset + 0] = r;
                this.pixelData[dataOffset + 1] = g;
                this.pixelData[dataOffset + 2] = b;

                this.changedPixels.Add((byte)channel);
                receivedUpdates++;
            }

            return SendStatus.NotSet;
        }

        public SendStatus SendPixelsValue(int startChannel, byte[] rgbx)
        {
            lock (lockObject)
            {
                if (!this.changedPixels.Any())
                    this.firstChange.Restart();

                int readOffset = 0;
                for (int i = 0; i < (int)(rgbx.Length / 4 + 1); i++)
                {
                    int dataOffset = 1 + (startChannel + i) * 4;

                    this.pixelData[dataOffset + 0] = rgbx[readOffset++];
                    this.pixelData[dataOffset + 1] = rgbx[readOffset++];
                    this.pixelData[dataOffset + 2] = rgbx[readOffset++];
                    readOffset++;

                    this.changedPixels.Add((byte)(startChannel + i));
                }
                receivedUpdates++;
            }

            return SendStatus.NotSet;
        }
    }
}
