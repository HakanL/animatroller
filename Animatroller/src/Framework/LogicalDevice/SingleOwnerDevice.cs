using System;
using System.Collections.Generic;
using System.Reactive.Disposables;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace Animatroller.Framework.LogicalDevice
{
    public abstract class SingleOwnerDevice : BaseDevice, IOwnedDevice, IReceivesData, ISendsData
    {
        public delegate void PushDataDelegate(DataElements dataElements, object value);

        protected List<IControlToken> owners;
        protected IControlToken currentOwner;
        protected ControlSubject<(int Channel, IData Data), IControlToken> outputData;
        protected ControlSubject<IData, IControlToken> currentOutputData;
        protected Subject<IData> outputChanged;
        private Dictionary<int, IData> ownerlessData;

        public SingleOwnerDevice(string name)
            : base(name)
        {
            base.newDataFunc = GetDefaultData;
            this.owners = new List<IControlToken>();
            this.outputData = new ControlSubject<(int, IData), IControlToken>((0, null), HasControl);
            this.currentOutputData = new ControlSubject<IData, IControlToken>(null, HasControl);
            this.outputChanged = new Subject<IData>();
            this.ownerlessData = new Dictionary<int, IData>();

            this.outputData.Subscribe(x =>
            {
                lock (this.lockObject)
                {
                    var data = PreprocessPushData(x.Data);

                    var usedKeys = new HashSet<DataElements>();

                    var dataList = CurrentData.Copy();

                    foreach (var kvp in data.ToList())
                    {
                        usedKeys.Add(kvp.Key);

                        switch (kvp.Key)
                        {
                            case DataElements.PixelBitmap:
                                dataList[kvp.Key] = new System.Drawing.Bitmap((System.Drawing.Bitmap)kvp.Value);
                                break;

                            default:
                                dataList[kvp.Key] = kvp.Value;
                                break;
                        }
                    }

                    dataList.Where(k => !usedKeys.Contains(k.Key)).ToList()
                        .ForEach(k => dataList.Remove(k.Key));

                    SetNewData(dataList, channel: x.Channel);

                    if(x.Channel == this.currentChannel)
                        this.outputChanged.OnNext(CurrentData);
                }

                if (x.Channel == this.currentChannel)
                    this.currentOutputData.OnNext(x.Data);
            });
        }

        public void SetCurrentChannel(int channel)
        {
            this.currentChannel = channel;

            RefreshOutput();
        }

        protected void RefreshOutput()
        {
            // FIXME: Should we lock the CurrentData here?
            this.outputChanged.OnNext(CurrentData);
        }

        public override void SetInitialState()
        {
            base.SetInitialState();

            this.outputChanged.OnNext(CurrentData);
        }

        internal IData GetOwnerlessData(int channel)
        {
            lock (this.lockObject)
            {
                if (!this.ownerlessData.TryGetValue(channel, out IData data))
                {
                    data = GetDefaultData();
                    this.ownerlessData[channel] = data;
                }

                return data;
            }
        }

        protected virtual IData PreprocessPushData(IData data)
        {
            return data;
        }

        public IPushDataController GetDataObserver(int channel, IControlToken token)
        {
            if (token == null)
                throw new ArgumentNullException("token");

            return new ControlledObserverData(token, this.outputData, token.GetDataForDevice(this, channel));
        }

        public IData GetFrameBuffer(int channel, IControlToken token, IReceivesData device)
        {
            if (token == null)
            {
                // Attempt to get from call context
                token = System.Runtime.Remoting.Messaging.CallContext.LogicalGetData("TOKEN") as IControlToken;

                if (token is GroupControlToken groupToken)
                {
                    if (!groupToken.LockAndGetDataFromDevice(this, channel))
                        token = null;
                }
            }

            if (token == null)
                return GetOwnerlessData(channel);

            return token.GetDataForDevice(device, channel);
        }

        public void PushOutput(int channel, IControlToken token)
        {
            if (token == null)
            {
                // Attempt to get from call context
                token = System.Runtime.Remoting.Messaging.CallContext.LogicalGetData("TOKEN") as IControlToken;

                if (token is GroupControlToken groupToken)
                {
                    if (!groupToken.LockAndGetDataFromDevice(this, channel))
                        token = null;
                }
            }

            IData data;

            if (token == null)
                data = GetOwnerlessData(channel);
            else
                data = token.GetDataForDevice(this, channel);

            if (data != null)
                this.outputData.OnNext((channel, data), token);
        }

        public void SetData(int channel, IControlToken token, IData data)
        {
            lock (this.lockObject)
            {
                var frame = GetFrameBuffer(channel, token, this);

                foreach (var kvp in data)
                    frame[kvp.Key] = kvp.Value;
            }

            PushOutput(channel, token);
        }

        public virtual IControlToken TakeControl(int channel = 0, int priority = 1, [System.Runtime.CompilerServices.CallerMemberName] string name = "")
        {
            lock (this.lockObject)
            {
                var ownerCandidate = new ControlledDevice(
                    name,
                    priority,
                    populate => BuildDefaultData(populate),
                    cToken =>
                    {
                        IData restoreData;
                        IControlToken nextOwner;

                        lock (this.lockObject)
                        {
                            this.owners.Remove(cToken);

                            nextOwner = this.owners.LastOrDefault();

                            if (nextOwner != null)
                                restoreData = nextOwner.GetDataForDevice(this, channel);
                            else
                                restoreData = GetOwnerlessData(channel);

                            restoreData = restoreData.Copy();

                            this.currentOwner = nextOwner;

                            Executor.Current.SetControlToken(this, nextOwner);
                        }

                        SetData(channel, nextOwner, restoreData);
                    });

                // Insert new owner
                lock (this.lockObject)
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

        private IData GetDefaultData()
        {
            var data = new Data();

            BuildDefaultData(data);

            return data;
        }

        public abstract void BuildDefaultData(IData data);

        public bool IsOwned
        {
            get { return this.currentOwner != null; }
        }

        public IObservable<IData> OutputData
        {
            get
            {
                return this.currentOutputData.AsObservable();
            }
        }

        public IObservable<IData> OutputChanged
        {
            get
            {
                return this.outputChanged.AsObservable();
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
