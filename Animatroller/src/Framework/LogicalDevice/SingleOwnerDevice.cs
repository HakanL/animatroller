using System;
using System.Reactive.Disposables;

namespace Animatroller.Framework.LogicalDevice
{
    public abstract class SingleOwnerDevice : BaseDevice, IOwnedDevice
    {
        public class ControlledDevice : IControlToken
        {
            private bool hasControl;
            private Action dispose;

            public ControlledDevice(string name, bool hasControl, Action dispose)
            {
                this.Name = name;
                this.hasControl = hasControl;
                this.dispose = dispose;
            }

            public void Dispose()
            {
                if (this.dispose != null)
                    this.dispose();
            }

            public string Name { get; private set; }

            public bool HasControl
            {
                get { return this.hasControl; }
            }
        }

        protected IControlToken currentOwner;
        protected int currentPriority;

        public SingleOwnerDevice(string name)
            : base(name)
        {
            this.currentPriority = -1;
        }

        public virtual IControlToken TakeControl(int priority = 1, [System.Runtime.CompilerServices.CallerMemberName] string name = "")
        {
            lock (this)
            {
                if (currentOwner != null && priority <= this.currentPriority)
                    // Already owned
                    return new ControlledDevice(name, false, null);

                this.currentPriority = priority;

                this.currentOwner = new ControlledDevice(name, true, () =>
                {
                    lock (this)
                    {
                        this.currentOwner = null;
                        this.currentPriority = -1;
                    }
                });

                return this.currentOwner;
            }
        }

        public bool HasControl(IControlToken checkOwner)
        {
            return this.currentOwner == null || checkOwner == this.currentOwner;
        }
    }
}
