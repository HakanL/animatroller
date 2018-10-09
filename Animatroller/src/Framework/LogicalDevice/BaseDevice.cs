using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace Animatroller.Framework.LogicalDevice
{
    public abstract class BaseDevice : ILogicalDevice, IApiVersion3
    {
        protected string name;
        protected bool persistState;
        protected Dictionary<IChannel, IData> currentData;
        protected object lockObject = new object();
        protected IChannel currentChannel;
        protected Func<IData> newDataFunc;

        public BaseDevice(string name, bool persistState = false)
        {
            this.name = name;
            this.persistState = persistState;
            this.currentData = new Dictionary<IChannel, IData>();
            this.currentChannel = Channel.Main;

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

        private IData GetNewData()
        {
            return newDataFunc == null ? new Data() : newDataFunc();
        }

        protected void SetNewData(IData newData, IChannel channel)
        {
            lock (this.lockObject)
            {
                this.currentData[channel ?? Channel.Main] = newData;
            }
        }

        public IData CurrentData
        {
            get
            {
                lock (this.lockObject)
                {
                    IChannel channel = this.currentChannel;

                    if (!this.currentData.TryGetValue(channel ?? Channel.Main, out IData data))
                    {
                        this.currentData[channel ?? Channel.Main] = data = GetNewData();
                    }

                    return data;
                }
            }
        }

        public T GetCurrentData<T>(DataElements dataElement)
        {
            return (T)GetCurrentData(dataElement);
        }


        public object GetCurrentData(DataElements dataElement)
        {
            object value;
            lock (this.lockObject)
            {
                CurrentData.TryGetValue(dataElement, out value);
            }

            return value;
        }
    }
}
