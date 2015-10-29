using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Drawing;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using NLog;
using Animatroller.Framework.Extensions;
using Animatroller.Framework.LogicalDevice.Event;

namespace Animatroller.Framework.LogicalDevice
{
    public abstract class Group<T> : BaseDevice, IOwnedDevice where T : IOwnedDevice
    {
        protected Util.GroupControlToken currentOwner;
        protected List<T> members;
        protected Stack<Util.GroupControlToken> owners;

        public Group([System.Runtime.CompilerServices.CallerMemberName] string name = "")
            : base(name)
        {
            this.members = new List<T>();
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

        protected IControlToken GetCurrentOrNewToken(out bool ownsToken)
        {
            var controlToken = Executor.Current.GetControlToken(this);

            if (controlToken == null)
            {
                controlToken = TakeControl();
                ownsToken = true;
            }
            else
                ownsToken = false;

            return controlToken;
        }

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
