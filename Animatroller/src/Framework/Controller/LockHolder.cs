using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NLog;

namespace Animatroller.Framework.Controller
{
    public abstract class LockHolder
    {
        private string name;
        private HashSet<IOwnedDevice> handleLocks;
        private Dictionary<IOwnedDevice, IControlToken> heldLocks;

        public LockHolder([System.Runtime.CompilerServices.CallerMemberName] string name = "")
        {
            this.name = name;

            this.handleLocks = new HashSet<IOwnedDevice>();

            // Default
            this.LockPriority = 1;
        }

        public string Name
        {
            get { return this.name; }
        }

        protected void Lock()
        {
            this.heldLocks = new Dictionary<IOwnedDevice, IControlToken>();
            foreach (var handleLock in this.handleLocks)
            {
                var control = handleLock.TakeControl(LockPriority, Name);

                Executor.Current.SetControlToken(handleLock, control);

                heldLocks.Add(handleLock, control);
            }
        }

        protected void Release()
        {
            foreach (var kvp in this.heldLocks)
            {
                Executor.Current.RemoveControlToken(kvp.Key);

                kvp.Value.Dispose();
            }
        }

        public int LockPriority { get; set; }

        protected void AddHandleLocks(params IOwnedDevice[] devices)
        {
            foreach (var device in devices)
                this.handleLocks.Add(device);
        }
    }
}
