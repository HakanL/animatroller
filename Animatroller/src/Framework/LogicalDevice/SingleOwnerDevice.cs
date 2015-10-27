using System;
using System.Collections.Generic;
using System.Reactive.Disposables;

namespace Animatroller.Framework.LogicalDevice
{
    public abstract class SingleOwnerDevice : BaseDevice, IOwnedDevice
    {
        protected Stack<IControlToken> owners;
        protected IControlToken currentOwner;
        protected List<Action> releaseActions;

        public SingleOwnerDevice(string name)
            : base(name)
        {
            this.owners = new Stack<IControlToken>();
            this.releaseActions = new List<Action>();
        }

        public abstract void SaveState(Dictionary<string, object> state);

        public abstract void RestoreState(Dictionary<string, object> state);

        public virtual IControlToken TakeControl(int priority = 1, bool executeReleaseAction = true, [System.Runtime.CompilerServices.CallerMemberName] string name = "")
        {
            lock (this)
            {
                if (this.currentOwner != null && priority <= this.currentOwner.Priority)
                    // Already owned (by us or someone else)
                    return ControlledDevice.Empty;

                var savedState = new Dictionary<string, object>();
                SaveState(savedState);

                var newOwner = new ControlledDevice(name, priority, savedState, s =>
                {
                    lock (this)
                    {
                        IControlToken oldOwner;

                        if (this.owners.Count > 0)
                            oldOwner = this.owners.Pop();
                        else
                            oldOwner = null;

                        this.currentOwner = oldOwner;

                        Executor.Current.SetControlToken(this, oldOwner);
                    }

                    if (executeReleaseAction)
                        this.releaseActions.ForEach(x => x());

                    if (s != null)
                        this.RestoreState(s);
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
