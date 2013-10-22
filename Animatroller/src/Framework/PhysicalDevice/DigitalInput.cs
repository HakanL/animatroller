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

        public DigitalInput Connect(LogicalDevice.DigitalInput logicalDevice, bool reverse = false)
        {
            StateChanged += (sender, e) =>
                {
                    if(reverse)
                        logicalDevice.Trigger(!e.NewState);
                    else
                        logicalDevice.Trigger(e.NewState);
                };

            return this;
        }

        public void StartDevice()
        {
        }
    }
}
