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
    public class TestPixel2D : INeedsMatrixLight, IPhysicalDevice
    {
        private object lockObject = new object();
        private string name;
        private bool pixelsChanged;
        private Color[,] pixelData;
        private Task senderTask;
        private System.Threading.CancellationTokenSource cancelSource;
        private int sentUpdates;
        private int receivedUpdates;
        private ILogicalDevice logicalDevice;
        private Control.MatrixLight control;
        private int numberOfPixels;
        private int pixelWidth;
        private int pixelHeight;

        public Control.MatrixLight LightControl
        {
            set
            {
                this.control = value;
            }
        }

        public TestPixel2D(IPixel2D logicalDevice)
        {
            this.name = logicalDevice.Name;
            this.pixelWidth = logicalDevice.PixelWidth;
            this.pixelHeight = logicalDevice.PixelHeight;

            Executor.Current.Register(this);

            this.logicalDevice = logicalDevice;

            logicalDevice.Output.Subscribe(x =>
                {
                    Array.Copy(x, this.pixelData, PixelWidth * PixelHeight);

                    this.pixelsChanged = true;
                });

            this.cancelSource = new System.Threading.CancellationTokenSource();
            this.pixelData = new Color[this.pixelWidth, this.pixelHeight];

            this.senderTask = new Task(x =>
            {
                while (!this.cancelSource.IsCancellationRequested)
                {
                    lock (lockObject)
                    {
                        if (this.control == null)
                            continue;

                        if (this.pixelsChanged)
                        {
                            this.pixelsChanged = false;

                            this.sentUpdates++;

                            // Send everything
                            control.SetPixels(this.pixelData);
                        }
                    }

                    System.Threading.Thread.Sleep(100);
                }
            }, this.cancelSource.Token, TaskCreationOptions.LongRunning);

            this.senderTask.Start();
        }

        public void SetInitialState()
        {
        }

        public string Name
        {
            get { return this.name; }
        }

        public int PixelWidth
        {
            get { return this.pixelWidth; }
        }

        public int PixelHeight
        {
            get { return this.pixelHeight; }
        }
    }
}
