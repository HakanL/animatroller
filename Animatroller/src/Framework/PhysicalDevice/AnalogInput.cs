using System;
using Animatroller.Framework.Extensions;
using Animatroller.Framework.LogicalDevice;

namespace Animatroller.Framework.PhysicalDevice
{
    public class AnalogInput : IPhysicalDevice
    {
        public Action<double> Trigger { get; private set; }

        private event EventHandler<LogicalDevice.Event.BrightnessChangedEventArgs> BrightnessChanged;

        public AnalogInput()
        {
            Executor.Current.Register(this);

            this.Trigger = new Action<double>(x =>
                {
                    var handler = BrightnessChanged;
                    if (handler != null)
                        handler(this, new LogicalDevice.Event.BrightnessChangedEventArgs(x));
                });
        }

        public AnalogInput Connect(LogicalDevice.AnalogInput logicalDevice)
        {
            BrightnessChanged += (sender, e) =>
                {
                    logicalDevice.Value = e.NewBrightness;
                };

            return this;
        }

        public void StartDevice()
        {
        }
    }
}
