using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using NLog;
using Animatroller.Framework.Controller;
using System.Threading.Tasks;

namespace Animatroller.Framework.Import
{
    public abstract class BaseImporter2
    {
        public class ChannelData
        {
            public string Name { get; private set; }

            public bool Mapped { get; set; }

            public ChannelData(string name)
            {
                this.Name = name;
            }
        }

/*        public class MappedDeviceDimmer
        {
            public IReceivesBrightness Device { get; set; }

            //public void RunEffect(Effect.IMasterBrightnessEffect effect, TimeSpan oneSweepDuration)
            //{
            //    this.Device.RunEffect(effect, oneSweepDuration);
            //}

            public MappedDeviceDimmer(IReceivesBrightness device)
            {
                this.Device = device;
            }
        }*/

        public class MappedDeviceRGB
        {
            public LogicalDevice.IHasColorControl Device { get; set; }

            public MappedDeviceRGB(LogicalDevice.IHasColorControl device)
            {
                this.Device = device;
            }
        }

        protected static Logger log = LogManager.GetCurrentClassLogger();
        protected Dictionary<IChannelIdentity, ChannelData> channelData;
        private List<IChannelIdentity> channels;
        protected Dictionary<IChannelIdentity, HashSet<IReceivesBrightness>> mappedDevices;
        protected Dictionary<RGBChannelIdentity, HashSet<MappedDeviceRGB>> mappedRGBDevices;
        protected HashSet<IControlledDevice> controlledDevices;

        public BaseImporter2()
        {
            this.channelData = new Dictionary<IChannelIdentity, ChannelData>();
            this.channels = new List<IChannelIdentity>();
            this.mappedDevices = new Dictionary<IChannelIdentity, HashSet<IReceivesBrightness>>();
            this.mappedRGBDevices = new Dictionary<RGBChannelIdentity, HashSet<MappedDeviceRGB>>();
            this.controlledDevices = new HashSet<IControlledDevice>();
        }

        public abstract Task Start();
        
        public abstract void Stop();

        public IEnumerable<IChannelIdentity> GetChannels
        {
            get
            {
                return this.channels;
            }
        }

        public string GetChannelName(IChannelIdentity channelIdentity)
        {
            var channel = this.channelData[channelIdentity];

            return channel.Name;
        }

        public IChannelIdentity ChannelIdentityFromName(string name)
        {
            foreach (var kvp in this.channelData)
            {
                if (kvp.Value.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                    return kvp.Key;
            }

            throw new KeyNotFoundException(string.Format("Channel {0} not found"));
        }

        protected void AddChannelData(IChannelIdentity channelIdentity, ChannelData data)
        {
            this.channelData[channelIdentity] = data;
            this.channels.Add(channelIdentity);
        }

        protected void InternalMapDevice(IChannelIdentity channelIdentity, IReceivesBrightness device)
        {
            HashSet<IReceivesBrightness> devices;
            if (!mappedDevices.TryGetValue(channelIdentity, out devices))
            {
                devices = new HashSet<IReceivesBrightness>();
                mappedDevices[channelIdentity] = devices;
            }
            devices.Add(device);

            this.channelData[channelIdentity].Mapped = true;

            if (device is IControlledDevice)
                this.controlledDevices.Add((IControlledDevice)device);
            if (device is LogicalDevice.IHasControlledDevice)
                this.controlledDevices.Add(((LogicalDevice.IHasControlledDevice)device).ControlledDevice);
        }

        protected void InternalMapDevice(RGBChannelIdentity channelIdentity, MappedDeviceRGB device)
        {
            HashSet<MappedDeviceRGB> devices;
            if (!mappedRGBDevices.TryGetValue(channelIdentity, out devices))
            {
                devices = new HashSet<MappedDeviceRGB>();
                mappedRGBDevices[channelIdentity] = devices;
            }
            devices.Add(device);

            this.channelData[channelIdentity.R].Mapped = true;
            this.channelData[channelIdentity.G].Mapped = true;
            this.channelData[channelIdentity.B].Mapped = true;

            if (device.Device is IControlledDevice)
                this.controlledDevices.Add((IControlledDevice)device.Device);
            if (device.Device is LogicalDevice.IHasControlledDevice)
                this.controlledDevices.Add(((LogicalDevice.IHasControlledDevice)device.Device).ControlledDevice);
        }

        public void MapDevice(IChannelIdentity channelIdentity, IReceivesBrightness device)
        {
            InternalMapDevice(channelIdentity, device);
        }

        //public T MapDevice<T>(IChannelIdentity channelIdentity, Func<string, T> logicalDevice) where T : LogicalDevice.IHasBrightnessControl
        //{
        //    string name = GetChannelName(channelIdentity);

        //    var device = logicalDevice.Invoke(name);

        //    MapDevice(channelIdentity, device);

        //    return device;
        //}

        public void MapDevice(
            IChannelIdentity channelIdentityR,
            IChannelIdentity channelIdentityG,
            IChannelIdentity channelIdentityB,
            LogicalDevice.IHasColorControl device)
        {
            var mappedDevice = new MappedDeviceRGB(device);
            InternalMapDevice(new RGBChannelIdentity(channelIdentityR, channelIdentityG, channelIdentityB), mappedDevice);

            device.Color = Color.Black;
        }

        public TDevice MapDevice<TDevice>(
            IChannelIdentity channelIdentityR,
            IChannelIdentity channelIdentityG,
            IChannelIdentity channelIdentityB,
            Func<string, TDevice> logicalDevice) where TDevice : LogicalDevice.IHasColorControl
        {
            string name = string.Format("{0}/{1}/{2}",
                GetChannelName(channelIdentityR), GetChannelName(channelIdentityG), GetChannelName(channelIdentityB));

            var device = logicalDevice.Invoke(name);

            MapDevice(channelIdentityR, channelIdentityG, channelIdentityB, device);

            return device;
        }
    }
}
