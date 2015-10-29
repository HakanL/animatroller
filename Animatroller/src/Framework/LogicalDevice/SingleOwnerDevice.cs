using System;
using System.Collections.Generic;
using System.Reactive.Disposables;
using System.Linq;
using System.Reactive.Linq;

namespace Animatroller.Framework.LogicalDevice
{
    public abstract class SingleOwnerDevice : BaseDevice, IOwnedDevice, IReceivesData, ISendsData
    {
        public delegate void PushDataDelegate(DataElements dataElements, object value);

        protected Stack<IControlToken> owners;
        protected IControlToken currentOwner;
        protected ControlSubject<IData, IControlToken> outputData;
        private IData ownerlessData;

        public SingleOwnerDevice(string name)
            : base(name)
        {
            this.owners = new Stack<IControlToken>();
            this.outputData = new ControlSubject<IData, IControlToken>(null, HasControl);
            this.ownerlessData = new Data();

            this.outputData.Subscribe(x =>
            {
                foreach (var kvp in x)
                    this.currentData[kvp.Key] = kvp.Value;
            });
        }

        public IData CurrentData
        {
            get { return this.currentData; }
        }

        protected override void UpdateOutput()
        {
            PushData();
        }

        public ControlledObserverData GetDataObserver(IControlToken token = null)
        {
            return new ControlledObserverData(token ?? GetCurrentOrNewToken(), this.outputData);
        }

        protected void PushData(DataElements dataElement, object value)
        {
            PushData(Tuple.Create(dataElement, value));
        }

        protected void PushData(params Tuple<DataElements, object>[] values)
        {
            var controlToken = Executor.Current.GetControlToken(this);

            PushDataDelegate pushDelegate;
            if (controlToken != null)
                pushDelegate = controlToken.PushData;
            else
                pushDelegate = (d, v) =>
                {
                    ownerlessData[d] = v;
                };

            var data = new Data();

            foreach (var kvp in values)
            {
                data[kvp.Item1] = kvp.Item2;

                pushDelegate(kvp.Item1, kvp.Item2);
            }

            this.outputData.OnNext(data, controlToken);
        }

        public virtual IControlToken TakeControl(int priority = 1, [System.Runtime.CompilerServices.CallerMemberName] string name = "")
        {
            lock (this)
            {
                //FIXME: Not sure if we need this
                if (this.currentOwner != null && priority <= this.currentOwner.Priority)
                    // Already owned (by us or someone else)
                    return ControlledDevice.Empty;

                var newOwner = new ControlledDevice(name, priority, () =>
                {
                    IData restoreData;

                    lock (this)
                    {
                        IControlToken oldOwner;

                        if (this.owners.Count > 0)
                            oldOwner = this.owners.Pop();
                        else
                            oldOwner = null;

                        if (oldOwner != null)
                            restoreData = oldOwner.Data;
                        else
                            restoreData = ownerlessData;

                        this.currentOwner = oldOwner;

                        Executor.Current.SetControlToken(this, oldOwner);
                    }

                    PushData(restoreData.Select(x => Tuple.Create(x.Key, x.Value)).ToArray());
                });

                // Push current owner
                this.owners.Push(this.currentOwner);

                this.currentOwner = newOwner;

                Executor.Current.SetControlToken(this, newOwner);

                return newOwner;
            }
        }

        public bool HasControl(IControlToken checkOwner)
        {
            var test = Executor.Current.GetControlToken(this);

            return this.currentOwner == null || checkOwner == this.currentOwner;
        }

        public bool HasControl()
        {
            return HasControl(Executor.Current.GetControlToken(this));
        }

        public bool IsOwned
        {
            get { return this.currentOwner != null; }
        }

        public IObservable<IData> OutputData
        {
            get
            {
                return this.outputData.DistinctUntilChanged();
            }
        }

        protected IControlToken GetCurrentOrNewToken()
        {
            var controlToken = Executor.Current.GetControlToken(this);

            if (controlToken == null)
                controlToken = TakeControl();

            return controlToken;
        }

        public void ReleaseOurLock()
        {
            var threadControlToken = Executor.Current.GetControlToken(this);

            if (threadControlToken == this.currentOwner)
            {
                // Release
                threadControlToken.Dispose();
            }
        }
    }
}
