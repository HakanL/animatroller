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
        public class GroupControlToken : IControlToken
        {
            internal Dictionary<T, IControlToken> MemberTokens { get; set; }
            private Action dispose;

            public GroupControlToken(Dictionary<T, IControlToken> memberTokens, Action dispose, int priority = 1)
            {
                MemberTokens = memberTokens;
                this.dispose = dispose;
                Priority = priority;
            }

            public int Priority { get; set; }

            public object State { get { return null; } }

            public void Dispose()
            {
                foreach (var memberToken in MemberTokens.Values)
                    memberToken.Dispose();
                MemberTokens.Clear();

                this.dispose();
            }
        }

        protected GroupControlToken currentOwner;
        protected List<T> members;
        protected Stack<GroupControlToken> owners;

        public Group([System.Runtime.CompilerServices.CallerMemberName] string name = "")
            : base(name)
        {
            this.members = new List<T>();
            this.owners = new Stack<GroupControlToken>();
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

        protected IControlToken GetCurrentOrNewToken()
        {
            var controlToken = Executor.Current.GetControlToken(this);

            if (controlToken == null)
                controlToken = TakeControl();

            return controlToken;
        }

        public IControlToken TakeControl(int priority = 1, bool executeReleaseAction = true, [System.Runtime.CompilerServices.CallerMemberName] string name = "")
        {
            lock (this.members)
            {
                if (currentOwner != null && priority <= this.currentOwner.Priority)
                    // Already owned (by us or someone else)
                    return ControlledDevice.Empty;

                var memberTokens = new Dictionary<T, IControlToken>();
                foreach (var device in this.members)
                    memberTokens.Add(device, device.TakeControl(priority, executeReleaseAction, name));

                var newOwner = new GroupControlToken(
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
            var test = Executor.Current.GetControlToken(this);

            return this.currentOwner == null || checkOwner == this.currentOwner;
        }

        public bool IsOwned
        {
            get { return this.currentOwner != null; }
        }
    }
}
