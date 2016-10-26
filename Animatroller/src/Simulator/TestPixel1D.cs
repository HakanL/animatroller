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
        private ILogicalDevice logicalDevice;
        private Control.PixelLight1D control;
        private int numberOfPixels;
        private bool newDataAvailable;

        private TestPixel1D(IUpdateActionParent parent, int numberOfPixels)
        {
            this.numberOfPixels = numberOfPixels;

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

        public TestPixel1D(IUpdateActionParent parent, IPixel1D2 logicalDevice)
            : this(parent, logicalDevice.Pixels)
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
