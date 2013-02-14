using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using NLog;
using Animatroller.Framework.Controller;

namespace Animatroller.Framework.Import
{
    public abstract class BaseImporter
    {
        public interface ISimpleInvokeEvent
        {
            void Invoke();
        }

        public class Timeline : Timeline<ISimpleInvokeEvent>
        {
            public Timeline(int? iterations)
                : base(iterations)
            {
            }
        }

        public class ChannelData
        {
            public string Name { get; private set; }
            public bool Mapped { get; set; }

            public ChannelData(string name)
            {
                this.Name = name;
            }
        }

        public class MappedDeviceDimmer
        {
            public LogicalDevice.IHasBrightnessControl Device { get; set; }

            public void RunEffect(Effect.IMasterBrightnessEffect effect, TimeSpan oneSweepDuration)
            {
                this.Device.RunEffect(effect, oneSweepDuration);
            }

            public MappedDeviceDimmer(LogicalDevice.IHasBrightnessControl device)
            {
                this.Device = device;
            }
        }

        public class MappedDeviceRGB
        {
            public LogicalDevice.IHasColorControl Device { get; set; }

            public MappedDeviceRGB(LogicalDevice.IHasColorControl device)
            {
                this.Device = device;
            }
        }

        protected static Logger log = LogManager.GetCurrentClassLogger();
        private Dictionary<IChannelIdentity, ChannelData> channelData;
        private List<IChannelIdentity> channels;
        protected Dictionary<IChannelIdentity, HashSet<MappedDeviceDimmer>> mappedDevices;
        protected Dictionary<RGBChannelIdentity, HashSet<MappedDeviceRGB>> mappedRGBDevices;
        protected HashSet<IControlledDevice> controlledDevices;

        public BaseImporter()
        {
            this.channelData = new Dictionary<IChannelIdentity, ChannelData>();
            this.channels = new List<IChannelIdentity>();
            this.mappedDevices = new Dictionary<IChannelIdentity, HashSet<MappedDeviceDimmer>>();
            this.mappedRGBDevices = new Dictionary<RGBChannelIdentity, HashSet<MappedDeviceRGB>>();
            this.controlledDevices = new HashSet<IControlledDevice>();
        }

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

        protected void AddChannelData(IChannelIdentity channelIdentity, ChannelData data)
        {
            this.channelData[channelIdentity] = data;
            this.channels.Add(channelIdentity);
        }

        protected void InternalMapDevice(IChannelIdentity channelIdentity, MappedDeviceDimmer device)
        {
            HashSet<MappedDeviceDimmer> devices;
            if (!mappedDevices.TryGetValue(channelIdentity, out devices))
            {
                devices = new HashSet<MappedDeviceDimmer>();
                mappedDevices[channelIdentity] = devices;
            }
            devices.Add(device);
         
            this.channelData[channelIdentity].Mapped = true;

            if (device.Device is IControlledDevice)
                this.controlledDevices.Add((IControlledDevice)device.Device);
            if (device.Device is LogicalDevice.IHasControlledDevice)
                this.controlledDevices.Add(((LogicalDevice.IHasControlledDevice)device.Device).ControlledDevice);
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

        public void MapDevice(IChannelIdentity channelIdentity, LogicalDevice.IHasBrightnessControl device)
        {
            var mappedDevice = new MappedDeviceDimmer(device);
            InternalMapDevice(channelIdentity, mappedDevice);

            device.Brightness = 0.0;
        }

        public T MapDevice<T>(IChannelIdentity channelIdentity, Func<string, T> logicalDevice) where T : LogicalDevice.IHasBrightnessControl
        {
            string name = GetChannelName(channelIdentity);

            var device = logicalDevice.Invoke(name);

            MapDevice(channelIdentity, device);

            return device;
        }

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

        public T MapDevice<T>(
            IChannelIdentity channelIdentityR,
            IChannelIdentity channelIdentityG,
            IChannelIdentity channelIdentityB,
            Func<string, T> logicalDevice) where T : LogicalDevice.IHasColorControl
        {
            string name = string.Format("{0}/{1}/{2}",
                GetChannelName(channelIdentityR), GetChannelName(channelIdentityG), GetChannelName(channelIdentityB));

            var device = logicalDevice.Invoke(name);

            MapDevice(channelIdentityR, channelIdentityG, channelIdentityB, device);

            return device;
        }

        protected Timeline InternalCreateTimeline(int? iterations)
        {
            foreach (var kvp in this.channelData)
            {
                if (!kvp.Value.Mapped)
                {
                    log.Warn("No devices mapped to {0}", kvp.Key);
                }
            }

            var timeline = new Timeline(iterations);
            timeline.MultiTimelineTrigger += (sender, e) =>
                {
                    foreach (var controlledDevice in this.controlledDevices)
                        controlledDevice.Suspend();
                    try
                    {
                        foreach (var invokeEvent in e.Code)
                            invokeEvent.Invoke();
                    }
                    finally
                    {
                        foreach (var controlledDevice in this.controlledDevices)
                            controlledDevice.Resume();
                    }
                };

            return timeline;
        }

        public abstract Timeline CreateTimeline(int? iterations);
    }
}
