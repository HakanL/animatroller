using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using Animatroller.Framework;
using Animatroller.Framework.Extensions;
using Animatroller.Framework.LogicalDevice;

namespace Animatroller.Simulator
{
    public class TestPixel1D : INeedsRopeLight, IPhysicalDevice
    {
        private object lockObject = new object();
        private HashSet<byte> changedPixels;
        private Color[] pixelData;
        private Task senderTask;
        private System.Threading.CancellationTokenSource cancelSource;
        private System.Diagnostics.Stopwatch firstChange;
        private int sentUpdates;
        private int receivedUpdates;
        private ILogicalDevice logicalDevice;
        private Control.RopeLight control;
        private int numberOfPixels;

        public Control.RopeLight LightControl
        {
            set
            {
                this.control = value;
            }
        }

        public int Pixels
        {
            get { return this.numberOfPixels; }
        }

        public TestPixel1D(IPixel1D logicalDevice)
        {
            Executor.Current.Register(this);

            this.logicalDevice = logicalDevice;
            this.numberOfPixels = logicalDevice.Pixels;

            logicalDevice.PixelChanged += (sender, e) =>
                {
                    var hsv = new HSV(e.NewColor);
                    hsv.Value = hsv.Value * e.NewBrightness;
                    Color c = hsv.Color;

                    lock (lockObject)
                    {
                        if (!this.changedPixels.Any())
                            this.firstChange.Restart();

                        this.pixelData[e.Channel] = c;

                        this.changedPixels.Add((byte)e.Channel);
                        receivedUpdates++;
                    }

                };

            logicalDevice.MultiPixelChanged += (sender, e) =>
                {
                    var newColors = new Color[e.NewValues.Length];
                    for(int i = 0; i < e.NewValues.Length; i++)
                    {
                        var hsv = new HSV(e.NewValues[i].Color);
                        hsv.Value = hsv.Value * e.NewValues[i].Brightness;
                        newColors[i] = hsv.Color;
                    }

                    lock (lockObject)
                    {
                        if (!this.changedPixels.Any())
                            this.firstChange.Restart();

                        int readOffset = 0;
                        for (int i = 0; i < newColors.Length; i++)
                        {
                            int dataOffset = e.StartChannel + i;

                            this.pixelData[dataOffset] = newColors[readOffset++];

                            this.changedPixels.Add((byte)(e.StartChannel + i));
                        }
                        receivedUpdates++;
                    }
                };

            this.changedPixels = new HashSet<byte>();
            this.cancelSource = new System.Threading.CancellationTokenSource();
            this.firstChange = new System.Diagnostics.Stopwatch();
            this.pixelData = new Color[this.numberOfPixels];

            this.senderTask = new Task(x =>
            {
                while (!this.cancelSource.IsCancellationRequested)
                {
                    lock (lockObject)
                    {
                        if (this.changedPixels.Any())
                        {
                            this.firstChange.Stop();
                            this.sentUpdates++;
                            //log.Info("Sending {0} changes to SIM. Oldest {1:N2}ms. Recv: {2}   Sent: {3}",
                            //    this.changedPixels.Count, this.firstChange.Elapsed.TotalMilliseconds,
                            //    receivedUpdates, sentUpdates);

                            if (this.changedPixels.Count <= 2)
                            {
                                foreach (var channel in this.changedPixels)
                                {
                                    control.SetPixel(channel, this.pixelData[channel]);
                                }
                            }
                            else
                            {
                                // Send everything
                                control.SetPixels(0, this.pixelData);
                            }
                            this.changedPixels.Clear();
                        }
                    }

                    System.Threading.Thread.Sleep(50);
                }
            }, this.cancelSource.Token, TaskCreationOptions.LongRunning);

            this.senderTask.Start();
        }

        public ILogicalDevice ConnectedDevice
        {
            get { return this.logicalDevice; }
        }

        public void SetInitialState()
        {
        }

        public string Name
        {
            get { return string.Empty; }
        }
    }
}
