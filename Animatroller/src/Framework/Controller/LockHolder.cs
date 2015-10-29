using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Animatroller.Framework.LogicalDevice.Util;
using NLog;

namespace Animatroller.Framework.Controller
{
    public abstract class LockHolder
    {
        private string name;
        private HashSet<IOwnedDevice> handleLocks;
        protected GroupControlToken groupControlToken;

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
            var heldLocks = new Dictionary<IOwnedDevice, IControlToken>();
            foreach (var handleLock in this.handleLocks)
            {
                var control = handleLock.TakeControl(priority: LockPriority, name: Name);

                heldLocks.Add(handleLock, control);
            }

            this.groupControlToken = new GroupControlToken(heldLocks, () =>
            {
            }, LockPriority);
        }

        protected void Release()
        {
            if (this.groupControlToken != null)
            {
                this.groupControlToken.Dispose();
                this.groupControlToken = null;
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
