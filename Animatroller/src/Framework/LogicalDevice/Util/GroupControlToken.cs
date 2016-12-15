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
            int priority = 1,
            [System.Runtime.CompilerServices.CallerMemberName] string name = "")
        {
            MemberTokens = memberTokens;
            this.disposeAction = disposeAction;
            this.ownsTokens = disposeLocks;
            Priority = priority;
            Name = name;
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
            Name = name;
        }

        public int Priority { get; set; }

        public string Name { get; private set; }

        public bool AutoAddDevices { get; set; }

        public IData GetDataForDevice(IOwnedDevice device)
        {
            IControlToken token;

            if (MemberTokens.TryGetValue(device, out token))
            {
                return token.GetDataForDevice(device);
            }

            if (AutoAddDevices)
            {
                token = device.TakeControl(priority: Priority, name: Name);
                Add(device, token);

                return token.GetDataForDevice(device);
            }

            var sod = device as SingleOwnerDevice;
            if (sod != null)
                return sod.GetOwnerlessData();

            throw new ArgumentException("Unhandled device");
        }

        public void Dispose()
        {
            if (this.ownsTokens)
            {
                foreach (var memberToken in MemberTokens.Values.ToList())
                    memberToken.Dispose();
            }

            if (this.disposeAction != null)
                this.disposeAction(this);
        }

        //public void PushData(DataElements dataElement, object value)
        //{
        //    foreach (var memberToken in MemberTokens.Values)
        //        memberToken.PushData(dataElement, value);
        //}

        public bool IsOwner(IControlToken checkToken)
        {
            return MemberTokens.ContainsValue(checkToken);
        }

        public void Add(IOwnedDevice device, IControlToken token)
        {
            MemberTokens.Add(device, token);
        }

        /// <summary>
        /// Lock if group token is configured for auto add
        /// </summary>
        /// <param name="device"></param>
        /// <returns>True if device is controlled by this group token</returns>
        public bool LockAndGetDataFromDevice(IOwnedDevice device)
        {
            if (MemberTokens.ContainsKey(device))
                return true;

            if (!AutoAddDevices)
                return false;

            // Add
            MemberTokens.Add(device, device.TakeControl(Priority, Name));

            return true;
        }
    }
}
