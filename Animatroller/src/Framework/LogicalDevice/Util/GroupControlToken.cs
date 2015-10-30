using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Animatroller.Framework.LogicalDevice
{
    public class GroupControlToken : IControlToken
    {
        internal Dictionary<IOwnedDevice, IControlToken> MemberTokens { get; private set; }
        private Action<IControlToken> disposeAction;
        private bool ownsTokens;

        public GroupControlToken(
            Dictionary<IOwnedDevice, IControlToken> memberTokens,
            bool disposeLocks = false,
            Action<IControlToken> disposeAction = null,
            int priority = 1)
        {
            MemberTokens = memberTokens;
            this.disposeAction = disposeAction;
            this.ownsTokens = disposeLocks;
            Priority = priority;
        }

        public GroupControlToken(IEnumerable<IOwnedDevice> devices, Action<IControlToken> disposeAction, string name, int priority = 1)
        {
            MemberTokens = new Dictionary<IOwnedDevice, IControlToken>();
            foreach (var device in devices)
            {
                MemberTokens.Add(device, device.TakeControl(priority, name));
            }
            this.disposeAction = disposeAction;
            this.ownsTokens = true;
            Priority = priority;
        }

        public int Priority { get; set; }

        public void Dispose()
        {
            if (this.ownsTokens)
            {
                foreach (var memberToken in MemberTokens.Values)
                    memberToken.Dispose();

                MemberTokens.Clear();
            }

            if (this.disposeAction != null)
                this.disposeAction(this);
        }

        public void PushData(DataElements dataElement, object value)
        {
            foreach (var memberToken in MemberTokens.Values)
                memberToken.PushData(dataElement, value);
        }

        public bool IsOwner(IControlToken checkToken)
        {
            return MemberTokens.ContainsValue(checkToken);
        }
    }
}
