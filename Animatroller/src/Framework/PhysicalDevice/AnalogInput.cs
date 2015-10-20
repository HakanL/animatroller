using System;
using Animatroller.Framework.Extensions;
using Animatroller.Framework.LogicalDevice;

namespace Animatroller.Framework.PhysicalDevice
{
    public class AnalogInput : IPhysicalDevice
    {
        public Action<double> Trigger { get; private set; }

        private event EventHandler<LogicalDevice.Event.BrightnessChangedEventArgs> ValueChanged;

        public AnalogInput()
        {
            Executor.Current.Register(this);

            this.Trigger = new Action<double>(x =>
                {
                    var handler = ValueChanged;
                    if (handler != null)
                        handler(this, new LogicalDevice.Event.BrightnessChangedEventArgs(x));
                });
        }

        public AnalogInput Connect(LogicalDevice.AnalogInput2 logicalDevice)
        {
            ValueChanged += (sender, e) =>
            {
                logicalDevice.Value = e.NewBrightness;
            };

            return this;
        }

        public AnalogInput Connect(LogicalDevice.AnalogInput3 logicalDevice)
        {
            ValueChanged += (sender, e) =>
            {
                logicalDevice.Value = e.NewBrightness;
            };

            return this;
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
