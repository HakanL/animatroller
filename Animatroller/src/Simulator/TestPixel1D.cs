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
        protected ColorBrightness[] pixelData;
        private object lockObject = new object();
        private HashSet<byte> changedPixels;
        private Task senderTask;
        private System.Threading.CancellationTokenSource cancelSource;
        private System.Diagnostics.Stopwatch firstChange;
        private ILogicalDevice logicalDevice;
        private Control.RopeLight control;
        private int numberOfPixels;
        private bool newDataAvailable;

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

        private TestPixel1D(int numberOfPixels)
        {
            Executor.Current.Register(this);

            this.numberOfPixels = numberOfPixels;
            this.changedPixels = new HashSet<byte>();
            this.cancelSource = new System.Threading.CancellationTokenSource();
            this.firstChange = new System.Diagnostics.Stopwatch();
            this.pixelData = new ColorBrightness[this.numberOfPixels];

            this.senderTask = new Task(x =>
            {
                while (!this.cancelSource.IsCancellationRequested)
                {
                    lock (lockObject)
                    {
                        if (this.newDataAvailable)
                        {
                            this.newDataAvailable = false;

                            control.SetPixels(0, this.pixelData);
                        }
                    }

                    System.Threading.Thread.Sleep(100);
                }
            }, this.cancelSource.Token, TaskCreationOptions.LongRunning);

            this.senderTask.Start();
        }

        public TestPixel1D(IPixel1D2 logicalDevice)
            : this(logicalDevice.Pixels)
        {
            this.logicalDevice = logicalDevice;

            logicalDevice.OutputChanged.Subscribe(x =>
            {
                SetFromIData(x);

                this.newDataAvailable = true;
            });

//            SetFromIData(logicalDevice.CurrentData);

            Executor.Current.Blackout.Subscribe(_ => this.newDataAvailable = true);
            Executor.Current.Whiteout.Subscribe(_ => this.newDataAvailable = true);
        }

        private void SetFromIData(IData data)
        {
            object value;

            if (data.TryGetValue(DataElements.PixelBrightness, out value))
            {
                double[] pixelBrightness = (double[])value;

                for (int i = 0; i < Math.Min(this.pixelData.Length, pixelBrightness.Length); i++)
                    this.pixelData[i].Brightness = pixelBrightness[i];
            }

            if (data.TryGetValue(DataElements.PixelColor, out value))
            {
                Color[] pixelColor = (Color[])value;
                for (int i = 0; i < Math.Min(this.pixelData.Length, pixelColor.Length); i++)
                    this.pixelData[i].Color = pixelColor[i];
            }
        }

        public TestPixel1D(IPixel1D logicalDevice)
            : this(logicalDevice.Pixels)
        {
            this.logicalDevice = logicalDevice;

            //logicalDevice.PixelChanged += (sender, e) =>
            //    {
            //        var hsv = new HSV(e.NewColor);
            //        hsv.Value = hsv.Value * e.NewBrightness;
            //        Color c = hsv.Color;

            //        lock (lockObject)
            //        {
            //            if (!this.changedPixels.Any())
            //                this.firstChange.Restart();

            //            this.pixelData[e.Channel] = c;

            //            this.changedPixels.Add((byte)e.Channel);
            //            receivedUpdates++;
            //        }

            //    };

            //logicalDevice.MultiPixelChanged += (sender, e) =>
            //    {
            //        var newColors = new Color[e.NewValues.Length];
            //        for (int i = 0; i < e.NewValues.Length; i++)
            //        {
            //            var hsv = new HSV(e.NewValues[i].Color);
            //            hsv.Value = hsv.Value * e.NewValues[i].Brightness;
            //            newColors[i] = hsv.Color;
            //        }

            //        lock (lockObject)
            //        {
            //            if (!this.changedPixels.Any())
            //                this.firstChange.Restart();

            //            int readOffset = 0;
            //            for (int i = 0; i < newColors.Length; i++)
            //            {
            //                int dataOffset = e.StartChannel + i;

            //                this.pixelData[dataOffset] = newColors[readOffset++];

            //                this.changedPixels.Add((byte)(e.StartChannel + i));
            //            }
            //            receivedUpdates++;
            //        }
            //    };
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
            get { return this.logicalDevice.Name; }
        }
    }
}
