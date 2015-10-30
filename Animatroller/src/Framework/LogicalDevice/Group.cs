using System;
using System.Collections.Generic;

namespace Animatroller.Framework.LogicalDevice
{
    public abstract class Group<T> : BaseDevice, IOwnedDevice where T : IReceivesData
    {
        protected Util.GroupControlToken currentOwner;
        protected List<T> members;
        protected Stack<Util.GroupControlToken> owners;

        public Group([System.Runtime.CompilerServices.CallerMemberName] string name = "")
            : base(name)
        {
            this.members = new List<T>();
            //TODO: Change to list instead of stack, like we do in SingleOwnerDevice
            this.owners = new Stack<Util.GroupControlToken>();
        }

        public void Add(params T[] devices)
        {
            lock (this.members)
            {
                this.members.AddRange(devices);
            }
        }

        protected override void UpdateOutput()
        {
            // No need to do anything here, each individual member should be started on its own
        }

        public ControlledObserverData GetDataObserver(IControlToken token)
        {
            if (token == null)
                throw new ArgumentNullException("token");

            var observers = new List<ControlledObserverData>();
            lock (this.members)
            {
                foreach (var member in this.members)
                {
                    observers.Add(member.GetDataObserver(token));
                }
            }

            var groupObserver = new ControlSubject<IData, IControlToken>(null, HasControl);
            groupObserver.Subscribe(x =>
            {
                foreach (var observer in observers)
                    observer.OnNext(x);
            });

            return new ControlledObserverData(token, groupObserver);
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

        public IControlToken TakeControl(int priority = 1, [System.Runtime.CompilerServices.CallerMemberName] string name = "")
        {
            lock (this.members)
            {
                if (currentOwner != null && priority <= this.currentOwner.Priority)
                    // Already owned (by us or someone else)
                    return ControlledDevice.Empty;

                var memberTokens = new Dictionary<IOwnedDevice, IControlToken>();
                foreach (var device in this.members)
                    memberTokens.Add(device, device.TakeControl(priority, name));

                var newOwner = new Util.GroupControlToken(
                    memberTokens,
                    () =>
                    {
                        lock (this.members)
                        {
                            if (this.owners.Count > 0)
                            {
                                this.currentOwner = this.owners.Pop();
                            }
                            else
                                this.currentOwner = null;

                            Executor.Current.SetControlToken(this, this.currentOwner);
                        }
                    },
                    priority);

                // Push current owner
                this.owners.Push(this.currentOwner);

                this.currentOwner = newOwner;

                Executor.Current.SetControlToken(this, newOwner);

                return this.currentOwner;
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
