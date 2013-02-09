using System;
using System.IO;
using System.Collections.Generic;
using System.Xml.Serialization;
using System.Linq;
using System.Drawing;
using NLog;
using LMS = Animatroller.Framework.Import.Schemas.LightORama.LMS;

namespace Animatroller.Framework.Utility
{
    public class LorImport
    {
        public class UnitCircuit
        {
            public int Unit { get; set; }
            public int Circuit { get; set; }

            public UnitCircuit(int unit, int circuit)
            {
                this.Unit = unit;
                this.Circuit = circuit;
            }
        }

        protected static Logger log = LogManager.GetCurrentClassLogger();
        protected Dictionary<Tuple<int, int>, HashSet<LogicalDevice.IHasBrightnessControl>> mappedDimmerDevices;
        protected Dictionary<Tuple<int, int, int, int>, HashSet<LogicalDevice.IHasColorControl>> mappedRGBDevices;
        protected LMS.sequence sequence;
        private Effect2.Shimmer shimmerEffect = new Effect2.Shimmer(0.5, 1.0);

        public LorImport(string filename)
        {
            this.mappedDimmerDevices = new Dictionary<Tuple<int, int>, HashSet<LogicalDevice.IHasBrightnessControl>>();
            this.mappedRGBDevices = new Dictionary<Tuple<int, int, int, int>, HashSet<LogicalDevice.IHasColorControl>>();

            var deserializer = new XmlSerializer(typeof(LMS.sequence));

            using (TextReader textReader = new StreamReader(filename))
            {
                sequence = (LMS.sequence)deserializer.Deserialize(textReader);
            }
        }

        public IEnumerable<int> AvailableUnits
        {
            get
            {
                var list = new HashSet<int>();

                foreach (var channel in sequence.channels)
                    list.Add(channel.unit);

                return list;
            }
        }

        public IEnumerable<int> GetCircuits(int unit)
        {
            var list = new HashSet<int>();

            foreach (var channel in sequence.channels)
            {
                if (channel.unit == unit)
                    list.Add(channel.circuit);
            }

            return list.OrderBy(x => x);
        }

        public IEnumerable<UnitCircuit> AvailableChannels
        {
            get
            {
                var list = new HashSet<Tuple<int, int>>();

                foreach (var channel in sequence.channels)
                    list.Add(new Tuple<int, int>(channel.unit, channel.circuit));

                return list.Select(x => new UnitCircuit(x.Item1, x.Item2));
            }
        }

        public string GetChannelName(int unit, int circuit)
        {
            foreach (var channel in sequence.channels)
            {
                if (channel.circuit == circuit && channel.unit == unit)
                    return channel.name;
            }

            throw new ArgumentOutOfRangeException("Circuit/Unit does not exist");
        }

        public System.Drawing.Color GetChannelColor(int unit, int circuit)
        {
            foreach (var channel in sequence.channels)
            {
                if (channel.circuit == circuit && channel.unit == unit)
                    return Color.FromArgb(int.Parse(channel.color));
            }

            throw new ArgumentOutOfRangeException("Circuit/Unit does not exist");
        }

        public LorImport MapDevice(int unit, int circuit, LogicalDevice.IHasBrightnessControl device)
        {
            var key = new Tuple<int, int>(unit, circuit);
            HashSet<LogicalDevice.IHasBrightnessControl> devices;
            if (!mappedDimmerDevices.TryGetValue(key, out devices))
            {
                devices = new HashSet<LogicalDevice.IHasBrightnessControl>();
                mappedDimmerDevices[key] = devices;
            }
            devices.Add(device);

            return this;
        }

        public T MapDevice<T>(int unit, int circuit, Func<string, T> logicalDevice) where T : LogicalDevice.IHasBrightnessControl
        {
            string name = GetChannelName(unit, circuit);

            var device = logicalDevice.Invoke(name);

            MapDevice(unit, circuit, device);

            return device;
        }

        public LorImport MapDevice(int unit, int circuitR, int circuitG, int circuitB, LogicalDevice.IHasColorControl device)
        {
            var key = new Tuple<int, int, int, int>(unit, circuitR, circuitG, circuitB);
            HashSet<LogicalDevice.IHasColorControl> devices;
            if (!mappedRGBDevices.TryGetValue(key, out devices))
            {
                devices = new HashSet<LogicalDevice.IHasColorControl>();
                mappedRGBDevices[key] = devices;
            }
            devices.Add(device);

            return this;
        }

        public T MapDevice<T>(int unit, int circuitR, int circuitG, int circuitB, Func<string, T> logicalDevice) where T : LogicalDevice.IHasColorControl
        {
            string name = string.Format("{0}/{1}/{2}", 
                GetChannelName(unit, circuitR), GetChannelName(unit, circuitG), GetChannelName(unit, circuitB));

            var device = logicalDevice.Invoke(name);

            MapDevice(unit, circuitR, circuitG, circuitB, device);

            return device;
        }

        // Light-O-Rama Musical Sequence
        public LorTimeline CreateTimeline()
        {
            var timeline = new LorTimeline();
            timeline.TimelineTrigger += timeline_TimelineTrigger;

            foreach (var channel in this.sequence.channels)
            {
                log.Info("Channel [{0}]   Unit: {1}   Circuit: {2}", channel.name, channel.unit, channel.circuit);

                HashSet<LogicalDevice.IHasBrightnessControl> devices;
                var key = new Tuple<int, int>(channel.unit, channel.circuit);
                if (!mappedDimmerDevices.TryGetValue(key, out devices))
                {
                    log.Warn("No devices mapped to unit {0}/circuit {1}, skipping", channel.unit, channel.circuit);
                    continue;
                }

                foreach (var effect in channel.effect)
                {
                    if (channel.deviceType != "LOR")
                        log.Warn("Not supporting device type {0} yet", channel.deviceType);

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

                    case "shimmer":
                        device.RunEffect(
                            shimmerEffect,
                            TimeSpan.FromMilliseconds((lorEvent.endCentisecond - lorEvent.startCentisecond) * 100));
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
