using System;
using System.Collections.Generic;
using System.Linq;

namespace Animatroller.Framework.LogicalDevice
{
    public class GroupControlToken : IControlToken
    {
        private readonly Dictionary<IOwnedDevice, IControlToken> memberTokens;
        private readonly Action<IControlToken> disposeAction;
        private readonly List<IControlToken> ownedTokens = new List<IControlToken>();

        public GroupControlToken(
            Dictionary<IOwnedDevice, IControlToken> memberTokens,
            bool disposeLocks = false,
            Action<IControlToken> disposeAction = null,
            int priority = 1,
            [System.Runtime.CompilerServices.CallerMemberName] string name = "")
        {
            this.memberTokens = memberTokens;
            this.disposeAction = disposeAction;
            if (disposeLocks)
                this.ownedTokens.AddRange(memberTokens.Select(x => x.Value));
            Priority = priority;
            Name = name;
        }

        public GroupControlToken(
            IEnumerable<IOwnedDevice> devices, Action<IControlToken> disposeAction,
            string name,
            IChannel channel = null,
            int priority = 1)
        {
            this.memberTokens = new Dictionary<IOwnedDevice, IControlToken>();
            foreach (var device in devices)
            {
                var token = device.TakeControl(channel, priority, name);
                this.memberTokens.Add(device, token);
                this.ownedTokens.Add(token);
            }
            this.disposeAction = disposeAction;
            Priority = priority;
            Name = name;
        }

        public GroupControlToken(
            Action<IControlToken> disposeAction = null,
            int priority = 1,
            [System.Runtime.CompilerServices.CallerMemberName] string name = "")
        {
            this.memberTokens = new Dictionary<IOwnedDevice, IControlToken>();
            this.disposeAction = disposeAction;
            Priority = priority;
            Name = name;
        }

        public int Priority { get; set; }

        public string Name { get; private set; }

        public bool AutoAddDevices { get; set; }

        public IData GetDataForDevice(IOwnedDevice device, IChannel channel)
        {
            IControlToken token;

            if (this.memberTokens.TryGetValue(device, out token))
            {
                return token.GetDataForDevice(device, channel);
            }

            if (AutoAddDevices)
            {
                token = device.TakeControl(channel: channel, priority: Priority, name: Name);
                Add(device, token);

                return token.GetDataForDevice(device, channel);
            }

            var sod = device as SingleOwnerDevice;
            if (sod != null)
                return sod.GetOwnerlessData(channel);

            throw new ArgumentException("Unhandled device");
        }

        public void Dispose()
        {
            foreach (var token in this.ownedTokens.ToList())
                token.Dispose();

            this.disposeAction?.Invoke(this);
        }

        public bool IsOwner(IControlToken checkToken)
        {
            return this.memberTokens.ContainsValue(checkToken);
        }

        public void Add(IOwnedDevice device, IControlToken token)
        {
            this.memberTokens.Add(device, token);
        }

        public void Add(IOwnedDevice device, IChannel channel = null)
        {
            var token = device.TakeControl(channel: null, priority: Priority, name: Name);
            this.ownedTokens.Add(token);
            this.memberTokens.Add(device, token);
        }

        public void AddRange(IChannel channel = null, params IOwnedDevice[] devices)
        {
            foreach (var device in devices)
            {
                var token = device.TakeControl(channel: null, priority: Priority, name: Name);
                this.ownedTokens.Add(token);
                this.memberTokens.Add(device, token);
            }
        }

        /// <summary>
        /// Lock if group token is configured for auto add
        /// </summary>
        /// <param name="device"></param>
        /// <returns>True if device is controlled by this group token</returns>
        public bool LockAndGetDataFromDevice(IOwnedDevice device, IChannel channel)
        {
            if (this.memberTokens.ContainsKey(device))
                return true;

            if (!AutoAddDevices)
                return false;

            // Add
            var token = device.TakeControl(channel, Priority, Name);
            this.ownedTokens.Add(token);
            this.memberTokens.Add(device, token);

            return true;
        }
    }
}
