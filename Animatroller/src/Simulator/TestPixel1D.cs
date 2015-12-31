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
        private Bitmap outputBitmap;
        private object lockObject = new object();
        private Task senderTask;
        private System.Threading.CancellationTokenSource cancelSource;
        private ILogicalDevice logicalDevice;
        private Control.PixelLight1D control;
        private int numberOfPixels;
        private bool newDataAvailable;

        private TestPixel1D(int numberOfPixels)
        {
            this.numberOfPixels = numberOfPixels;
            this.cancelSource = new System.Threading.CancellationTokenSource();

            this.senderTask = new Task(x =>
            {
                while (!this.cancelSource.IsCancellationRequested)
                {
                    lock (lockObject)
                    {
                        if (this.newDataAvailable)
                        {
                            this.newDataAvailable = false;

                            if (control != null)
                                control.SetImage(this.outputBitmap);
                        }
                    }

                    System.Threading.Thread.Sleep(100);
                }
            }, this.cancelSource.Token, TaskCreationOptions.LongRunning);

            this.senderTask.Start();

            Executor.Current.Register(this);
        }

        public TestPixel1D(IPixel1D2 logicalDevice)
            : this(logicalDevice.Pixels)
        {
            this.logicalDevice = logicalDevice;

            logicalDevice.ImageChanged.Subscribe(x =>
            {
                this.outputBitmap = new Bitmap(x);

                this.newDataAvailable = true;
            });
        }

        public Control.PixelLight1D LightControl
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
