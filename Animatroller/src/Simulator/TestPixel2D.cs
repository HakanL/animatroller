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
        private Bitmap outputBitmap;
        private object lockObject = new object();
        private ILogicalDevice logicalDevice;
        private Control.PixelLight2D control;
        private int pixelWidth;
        private int pixelHeight;
        private bool newDataAvailable;

        public TestPixel2D(IUpdateActionParent parent, int pixelWidth, int pixelHeight)
        {
            this.pixelWidth = pixelWidth;
            this.pixelHeight = pixelHeight;

            parent.AddUpdateAction(() =>
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
            });

            Executor.Current.Register(this);
        }

        public TestPixel2D(IUpdateActionParent parent, IPixel2D2 logicalDevice)
            : this(parent, logicalDevice.PixelWidth, logicalDevice.PixelHeight)
        {
            this.logicalDevice = logicalDevice;

            logicalDevice.ImageChanged.Subscribe(x =>
            {
                this.outputBitmap = new Bitmap(x);

                this.newDataAvailable = true;
            });
        }

        public Control.PixelLight2D LightControl
        {
            set
            {
                this.control = value;
            }
        }

        public int PixelWidth
        {
            get { return this.pixelWidth; }
        }

        public int PixelHeight
        {
            get { return this.pixelHeight; }
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
