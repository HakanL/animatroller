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
    public abstract class Group<T> : SingleOwnerDevice where T : IOwnedDevice
    {
        protected List<T> members;
        protected Dictionary<T, IControlToken> memberControlTokens;

        public Group([System.Runtime.CompilerServices.CallerMemberName] string name = "")
            : base(name)
        {
            this.members = new List<T>();
            this.memberControlTokens = new Dictionary<T, IControlToken>();
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

        public override IControlToken TakeControl(int priority = 1, [System.Runtime.CompilerServices.CallerMemberName] string name = "")
        {
            lock (this.members)
            {
                if (currentOwner != null && priority <= this.currentPriority)
                    // Already owned
                    return new ControlledDevice(name, false, null);

                this.currentPriority = priority;

                this.currentOwner = new ControlledDevice(name, true, () =>
                {
                    lock (this.members)
                    {
                        foreach (var memberControlToken in this.memberControlTokens.Values)
                        {
                            memberControlToken.Dispose();
                        }
                        this.memberControlTokens.Clear();

                        this.currentOwner = null;
                        this.currentPriority = -1;
                    }
                });

                foreach (var member in this.members)
                {
                    var memberControlToken = member.TakeControl(priority, name);

                    this.memberControlTokens.Add(member, memberControlToken);
                }

                return this.currentOwner;
            }
        }
    }
}
