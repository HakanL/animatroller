using System;

namespace Animatroller.Framework.LogicalDevice
{
    public abstract class BaseDevice : ILogicalDevice
    {
        protected string name;
        protected bool persistState;

        public BaseDevice(string name, bool persistState = false)
        {
            this.name = name;
            this.persistState = persistState;

            Executor.Current.Register(this);
        }

        public string Name
        {
            get { return this.name; }
        }

        public virtual void StartDevice()
        {
        }
    }
}
