using System;
using System.Reactive.Subjects;

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
            UpdateOutput();
        }

        protected abstract void UpdateOutput();

        public override string ToString()
        {
            return string.Format("{0} ({1})", Name, GetType().Name);
        }
    }
}
