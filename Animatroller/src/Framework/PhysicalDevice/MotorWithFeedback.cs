using System;
using Animatroller.Framework.Extensions;
using Animatroller.Framework.LogicalDevice;

namespace Animatroller.Framework.PhysicalDevice
{
    public class MotorWithFeedback : IPhysicalDevice
    {
        private Action<int, double, TimeSpan> physicalTrigger;
        public Action<int?, bool> Trigger { get; private set; }

        private event EventHandler<LogicalDevice.Event.MotorStatusChangedEventArgs> StatusChanged;

        public MotorWithFeedback(Action<int, double, TimeSpan> physicalTrigger)
        {
            Executor.Current.Register(this);

            this.physicalTrigger = physicalTrigger;

            this.Trigger = new Action<int?, bool>((newPos, failed) =>
                {
                    var handler = StatusChanged;
                    if (handler != null)
                        handler(this, new LogicalDevice.Event.MotorStatusChangedEventArgs(newPos, failed));
                });
        }

        public MotorWithFeedback Connect(LogicalDevice.MotorWithFeedback logicalDevice)
        {
            StatusChanged += (sender, e) =>
            {
                logicalDevice.Trigger(e.NewPos, e.Failed);
            };

            logicalDevice.VectorChanged += (sender, e) =>
                {
                    this.physicalTrigger(e.Vector.Target, e.Vector.Speed, e.Vector.Timeout);
                };

            return this;
        }

        public void StartDevice()
        {
        }

        public string Name
        {
            get { return string.Empty; }
        }
    }
}
