using System;
using System.IO;
using System.Collections.Generic;
using System.Xml.Serialization;
using NLog;
using LMS = Animatroller.Framework.Import.Schemas.LightORama.LMS;

namespace Animatroller.Framework.Utility
{
    public class LorImport
    {
        protected static Logger log = LogManager.GetCurrentClassLogger();
        protected Dictionary<Tuple<int, int>, HashSet<LogicalDevice.IHasBrightnessControl>> mappedDevices;
        protected LMS.sequence sequence;

        public LorImport(string filename)
        {
            this.mappedDevices = new Dictionary<Tuple<int, int>, HashSet<LogicalDevice.IHasBrightnessControl>>();

            var deserializer = new XmlSerializer(typeof(LMS.sequence));

            using (TextReader textReader = new StreamReader(filename))
            {
                sequence = (LMS.sequence)deserializer.Deserialize(textReader);
            }
        }

        public string GetChannelName(int circuit, int unit)
        {
            foreach (var channel in sequence.channels)
            {
                if (channel.circuit == circuit && channel.unit == unit)
                    return channel.name;
            }

            throw new ArgumentOutOfRangeException("Circuit/Unit does not exist");
        }

        public LorImport MapDevice(int circuit, int unit, LogicalDevice.IHasBrightnessControl device)
        {
            var key = new Tuple<int, int>(circuit, unit);
            HashSet<LogicalDevice.IHasBrightnessControl> devices;
            if (!mappedDevices.TryGetValue(key, out devices))
            {
                devices = new HashSet<LogicalDevice.IHasBrightnessControl>();
                mappedDevices[key] = devices;
            }
            devices.Add(device);

            return this;
        }

        public T MapDevice<T>(int circuit, int unit, Func<string, T> logicalDevice) where T : LogicalDevice.IHasBrightnessControl
        {
            string name = GetChannelName(circuit, unit);

            var device = logicalDevice.Invoke(name);

            MapDevice(circuit, unit, device);

            return device;
        }

        // Light-O-Rama Musical Sequence
        public LorTimeline CreateTimeline()
        {
            var timeline = new LorTimeline();
            timeline.TimelineTrigger += timeline_TimelineTrigger;

            foreach (var channel in sequence.channels)
            {
                log.Info("Channel [{0}]   Circuit: {1}   Unit: {2}", channel.name, channel.circuit, channel.unit);

                HashSet<LogicalDevice.IHasBrightnessControl> devices;
                var key = new Tuple<int, int>(channel.circuit, channel.unit);
                if (!mappedDevices.TryGetValue(key, out devices))
                {
                    log.Warn("No devices mapped to circuit {0}/unit {1}, skipping", channel.circuit, channel.unit);
                    continue;
                }

                foreach (var effect in channel.effect)
                {
                    var lorEvent = new LOREvent(devices, effect);

                    timeline.Add((double)effect.startCentisecond / 10, lorEvent);
                }
            }

            return timeline;
        }

        private void timeline_TimelineTrigger(object sender, LorTimeline.TimelineEventArgs e)
        {
            var lorEvent = e.Code;

            foreach (var device in lorEvent.Devices)
            {
                switch (lorEvent.type)
                {
                    case "intensity":
                        if (string.IsNullOrEmpty(lorEvent.startIntensity))
                        {
                            device.Brightness = (double)lorEvent.intensity / 100;
                        }
                        else
                        {
                            device.RunEffect(
                                new Effect2.Fader(
                                    double.Parse(lorEvent.startIntensity) / 100,
                                    double.Parse(lorEvent.endIntensity) / 100),
                                TimeSpan.FromMilliseconds((lorEvent.endCentisecond - lorEvent.startCentisecond) * 100));
                        }
                        break;

                    default:
                        log.Warn("Unknown type {0}", lorEvent.type);
                        break;
                }
            }
        }
    }

    public class LOREvent : LMS.channelsChannelEffect
    {
        public HashSet<LogicalDevice.IHasBrightnessControl> Devices { get; private set; }

        public LOREvent(HashSet<LogicalDevice.IHasBrightnessControl> devices, LMS.channelsChannelEffect effect)
        {
            Devices = devices;
            base.endCentisecond = effect.endCentisecond;
            base.endIntensity = effect.endIntensity;
            base.intensity = effect.intensity;
            base.startCentisecond = effect.startCentisecond;
            base.startIntensity = effect.startIntensity;
            base.type = effect.type;
        }
    }

    public class LorTimeline : Timeline<LOREvent>
    {
    }
}
