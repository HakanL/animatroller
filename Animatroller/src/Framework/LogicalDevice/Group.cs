using System;
using System.Collections.Generic;
using System.Linq;

namespace Animatroller.Framework.LogicalDevice
{
    public abstract class Group<T> : BaseDevice, IOwnedDevice where T : IReceivesData
    {
        protected IControlToken currentOwner;
        protected List<T> members;
        protected List<IControlToken> owners;
        protected IControlToken internalLock;

        public Group([System.Runtime.CompilerServices.CallerMemberName] string name = "")
            : base(name)
        {
            this.members = new List<T>();
            this.owners = new List<IControlToken>();
        }

        public void Add(params T[] devices)
        {
            lock (this.members)
            {
                this.members.AddRange(devices);
            }
        }

        protected List<T> AllMembers
        {
            get
            {
                lock (this.members)
                {
                    return this.members.ToList();
                }
            }
        }

        public void PushOutput(IChannel channel, IControlToken token)
        {
            foreach (var member in AllMembers)
            {
                member.PushOutput(channel, token);
            }
        }

        public IData GetFrameBuffer(IChannel channel, IControlToken token, IReceivesData device)
        {
            return device.GetFrameBuffer(channel, token, device);
        }

        protected override void UpdateOutput()
        {
            // No need to do anything here, each individual member should be started on its own
        }

        public IPushDataController GetDataObserver(IChannel channel, IControlToken token)
        {
            if (token == null)
                throw new ArgumentNullException("token");

            var observers = new List<IPushDataController>();
            lock (this.members)
            {
                foreach (var member in this.members)
                {
                    observers.Add(member.GetDataObserver(channel, token));
                }
            }

            var groupObserver = new ControlSubject<IData, IControlToken>(null, HasControl);
            groupObserver.Subscribe(x =>
            {
                foreach (var observer in observers)
                {
                    observer.SetDataFromIData(x);
                    observer.PushData(channel);
                }
            });

            return new ControlledGroupData(token, groupObserver);
        }

        public void TakeAndHoldControl(IChannel channel = null, int priority = 1, [System.Runtime.CompilerServices.CallerMemberName] string name = "")
        {
            if (this.internalLock != null)
            {
                this.internalLock.Dispose();
            }

            this.internalLock = TakeControl(channel, priority, name);
        }

        public void ReleaseControl()
        {
            if (this.internalLock != null)
            {
                this.internalLock.Dispose();
                this.internalLock = null;
            }
        }

        public IControlToken TakeControl(IChannel channel = null, int priority = 1, [System.Runtime.CompilerServices.CallerMemberName] string name = "")
        {
            lock (this.members)
            {
                var memberTokens = new Dictionary<IOwnedDevice, IControlToken>();
                foreach (var device in this.members)
                    memberTokens.Add(device, device.TakeControl(channel, priority, name));

                var ownerCandidate = new GroupControlToken(
                    memberTokens,
                    true,
                    cToken =>
                    {
                        IControlToken nextOwner;

                        lock (this.members)
                        {
                            this.owners.Remove(cToken);

                            nextOwner = this.owners.LastOrDefault();

                            this.currentOwner = nextOwner;

                            Executor.Current.SetControlToken(this, nextOwner);
                        }
                    },
                    priority: priority,
                    name: name);

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
            // Checked on an individual level instead
            return true;
        }

        public bool IsOwned
        {
            get { return this.currentOwner != null; }
        }
    }
}
