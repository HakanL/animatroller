using System;
using System.IO;
using System.Collections.Generic;
using System.Xml.Serialization;
using System.Linq;
using System.Drawing;
using NLog;
using Animatroller.Framework.Extensions;

namespace Animatroller.Framework.Controller
{
    public interface IMultiChannelTimelineEvent
    {
        void Invoke();
    }

    //public class MultiChannelTimeline : Timeline<IMultiChannelTimelineEvent>
    //{
    //    public MultiChannelTimeline(bool loop)
    //        : base(loop)
    //    {
    //    }
    //}

    public class MultiChannelTimeline
    {
        public class ChannelData
        {
            public string Name { get; private set; }

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
        protected Dictionary<IChannelIdentity, ChannelData> channelData;
        protected Dictionary<IChannelIdentity, HashSet<MappedDeviceDimmer>> mappedDevices;
        protected Dictionary<RGBChannelIdentity, HashSet<MappedDeviceRGB>> mappedRGBDevices;
        protected int effectsPerChannel;
        protected int eventPeriodInMilliseconds;

        public MultiChannelTimeline()
        {
            this.channelData = new Dictionary<IChannelIdentity, ChannelData>();
            this.mappedDevices = new Dictionary<IChannelIdentity, HashSet<MappedDeviceDimmer>>();
            this.mappedRGBDevices = new Dictionary<RGBChannelIdentity, HashSet<MappedDeviceRGB>>();
        }

        public IEnumerable<IChannelIdentity> GetChannels
        {
            get
            {
                return this.channelData.Keys;
            }
        }

        public string GetChannelName(IChannelIdentity channelIdentity)
        {
            var channel = this.channelData[channelIdentity];

            return channel.Name;
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

        public Timeline<IMultiChannelTimelineEvent> CreateTimeline(bool loop)
        {
            var timeline = new Timeline<IMultiChannelTimelineEvent>(loop);
            timeline.MultiTimelineTrigger += timeline_MultiTimelineTrigger;

            return timeline;
        }

        private void timeline_MultiTimelineTrigger(object sender, Timeline<IMultiChannelTimelineEvent>.MultiTimelineEventArgs e)
        {
            foreach (var invokeEvent in e.Code)
                invokeEvent.Invoke();
        }
    }
}
