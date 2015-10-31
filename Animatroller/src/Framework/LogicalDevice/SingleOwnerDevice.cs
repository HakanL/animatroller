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

        protected List<IControlTokenDevice> owners;
        protected IControlToken currentOwner;
        protected ControlSubject<IData, IControlToken> outputData;
        private IData ownerlessData;

        public SingleOwnerDevice(string name)
            : base(name)
        {
            this.owners = new List<IControlTokenDevice>();
            this.outputData = new ControlSubject<IData, IControlToken>(null, HasControl);
            this.ownerlessData = new Data();

            this.outputData.Subscribe(x =>
            {
                var data = PreprocessPushData(x);

                foreach (var kvp in data)
                    this.currentData[kvp.Key] = kvp.Value;
            });
        }

        public override void SetInitialState()
        {
            base.SetInitialState();

            // Copy current data to ownerless for initial state
            foreach (var kvp in this.currentData)
                this.ownerlessData[kvp.Key] = kvp.Value;
        }

        protected virtual IData PreprocessPushData(IData data)
        {
            return data;
        }

        public IData CurrentData
        {
            get { return this.currentData; }
        }

        protected override void UpdateOutput()
        {
            PushData(Executor.Current.GetControlToken(this));
        }

        public ControlledObserverData GetDataObserver(IControlToken token)
        {
            if (token == null)
                throw new ArgumentNullException("token");

            return new ControlledObserverData(token, this.outputData);
        }

        protected void PushData(DataElements dataElement, object value, IControlToken token)
        {
            PushData(token, Tuple.Create(dataElement, value));
        }

        protected void PushData(IControlToken token, IData data)
        {
            PushDataDelegate pushDelegate;
            if (token != null)
                pushDelegate = token.PushData;
            else
                pushDelegate = (d, v) =>
                {
                    ownerlessData[d] = v;
                };

            foreach (var kvp in data)
                pushDelegate(kvp.Key, kvp.Value);

            this.outputData.OnNext(data, token);
        }

        protected void PushData(IControlToken token, params Tuple<DataElements, object>[] values)
        {
            var data = new Data();

            foreach (var kvp in values)
                data[kvp.Item1] = kvp.Item2;

            PushData(token, data);
        }

        public virtual IControlToken TakeControl(int priority = 1, [System.Runtime.CompilerServices.CallerMemberName] string name = "")
        {
            lock (this)
            {
                var ownerCandidate = new ControlledDevice(name, priority, cToken =>
                {
                    IData restoreData;
                    IControlTokenDevice nextOwner;

                    lock (this)
                    {
                        this.owners.Remove(cToken);

                        nextOwner = this.owners.LastOrDefault();

                        if (nextOwner != null)
                            restoreData = nextOwner.Data;
                        else
                            restoreData = ownerlessData;

                        this.currentOwner = nextOwner;

                        Executor.Current.SetControlToken(this, nextOwner);
                    }

                    PushData(nextOwner, restoreData.Select(x => Tuple.Create(x.Key, x.Value)).ToArray());
                });

                // Insert new owner
                lock (this)
                {
                    int pos = -1;
                    for (int i = 0; i < this.owners.Count; i++)
                    {
                        if (this.owners[i].Priority < priority)
                            continue;

                        pos = i;
                        break;
                    }
                    if (pos == -1)
                        this.owners.Add(ownerCandidate);
                    else
                        this.owners.Insert(pos, ownerCandidate);
                }

                // Grab the owner with the highest priority (doesn't have to be the candidate)
                var newOwner = this.owners.Last();

                this.currentOwner = newOwner;

                Executor.Current.SetControlToken(this, newOwner);

                return ownerCandidate;
            }
        }

        public bool HasControl(IControlToken checkOwner)
        {
            return this.currentOwner == null || (checkOwner != null && checkOwner.IsOwner(this.currentOwner));
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

        //protected IControlToken GetCurrentOrNewToken(out bool ownsToken)
        //{
        //    var controlToken = Executor.Current.GetControlToken(this);

        //    if (controlToken == null)
        //    {
        //        controlToken = TakeControl();
        //        ownsToken = true;
        //    }
        //    else
        //        ownsToken = false;

        //    return controlToken;
        //}

        //public void ReleaseOurLock()
        //{
        //    var threadControlToken = Executor.Current.GetControlToken(this);

        //    if (threadControlToken == this.currentOwner)
        //    {
        //        // Release
        //        threadControlToken.Dispose();
        //    }
        //}
    }
}
