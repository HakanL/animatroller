using System;
using Animatroller.Framework.Extensions;
using Animatroller.Framework.LogicalDevice;

namespace Animatroller.Framework.PhysicalDevice
{
    public class DigitalInput : IPhysicalDevice
    {
        public Action<bool> Trigger { get; private set; }

        private event EventHandler<LogicalDevice.Event.StateChangedEventArgs> StateChanged;

        public DigitalInput()
        {
            Executor.Current.Register(this);

            this.Trigger = new Action<bool>(x =>
                {
                    var handler = StateChanged;
                    if (handler != null)
                        handler(this, new LogicalDevice.Event.StateChangedEventArgs(x));
                });
        }

        public DigitalInput Connect(LogicalDevice.DigitalInput logicalDevice)
        {
            StateChanged += (sender, e) =>
                {
                    logicalDevice.Trigger(e.NewState);
                };

            return this;
        }

        public void StartDevice()
        {
        }
    }
}
