//#define TRACE_IDATA

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
    public class TestLight : Animatroller.Framework.PhysicalDevice.BaseStrobeLight, INeedsLabelLight, IUpdateableControl
    {
        private Control.StrobeBulb control;
        private double? pan;
        private double? tilt;
        private object lockObject = new object();
        private bool performUpdate;
        private bool hasNewData;

        public Control.StrobeBulb LabelLightControl
        {
            set { this.control = value; }
        }

        public TestLight(IUpdateActionParent parent, IApiVersion3 logicalDevice)
            : base(logicalDevice)
        {
            parent.AddUpdateAction(() =>
            {
                lock (lockObject)
                {
                    if (this.performUpdate)
                    {
                        this.performUpdate = false;

                        if(this.hasNewData)
                            this.control.Invalidate();
                    }
                }
            });
        }

        protected override void SetFromIData(ILogicalDevice logicalDevice, IData data)
        {
            base.SetFromIData(logicalDevice, data);

            object value;
            if (data.TryGetValue(DataElements.Pan, out value))
                this.pan = ((double)value).Limit(0, 540);

            if (data.TryGetValue(DataElements.Tilt, out value))
                this.tilt = ((double)value).Limit(0, 270);

#if TRACE_IDATA
            int id = 0;
            foreach (var kvp in data)
            {
                id++;
                this.log.Verbose("{3} {0}. {1} = {2}", id, kvp.Key, kvp.Value, this.Name);
            }
#endif
        }

        protected override void Output()
        {
            this.control.Color = GetColorFromColorBrightness();
            string ownedStatus = string.Empty;

            foreach (ILogicalDevice device in this.logicalDevices)
            {
                if (device is IOwnedDevice && ((IOwnedDevice)device).IsOwned)
                {
                    ownedStatus = "*";
                    break;
                }
            }

            this.control.Text = string.Format("{1}{0:0%}", GetMonochromeBrightnessFromColorBrightness(), ownedStatus);

            this.control.ColorGel = this.colorBrightness.Color;
            this.control.Intensity = this.colorBrightness.Brightness;

            this.control.Pan = this.pan;
            this.control.Tilt = this.tilt;

            this.hasNewData = true;
        }

        public void Update()
        {
            this.performUpdate = true;
        }
    }
}
