using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace Animatroller.Framework.LogicalDevice
{
    public abstract class BaseDevice : ILogicalDevice, IApiVersion3
    {
        protected string name;
        protected bool persistState;
        protected IData currentData;
        protected object lockObject = new object();

        public BaseDevice(string name, bool persistState = false)
        {
            this.name = name;
            this.persistState = persistState;
            this.currentData = new Data();

            Executor.Current.Register(this);
        }

        public string Name
        {
            get { return this.name; }
        }

        public virtual void SetInitialState()
        {
        }

        public virtual void EnableOutput()
        {
            UpdateOutput();
        }

        protected virtual void UpdateOutput()
        {
        }

        public override string ToString()
        {
            return string.Format("{0} ({1})", Name, GetType().Name);
        }

        public IData CurrentData
        {
            get { return this.currentData; }
        }

        public object GetCurrentData(DataElements dataElement)
        {
            object value;
            lock (this.lockObject)
            {
                this.currentData.TryGetValue(dataElement, out value);
            }

            return value;
        }
    }
}
