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

        public DigitalInput Connect(LogicalDevice.DigitalInput2 logicalDevice, bool inverted = false)
        {
            StateChanged += (sender, e) =>
            {
                if (inverted)
                    logicalDevice.Control.OnNext(!e.NewState);
                else
                    logicalDevice.Control.OnNext(e.NewState);
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
