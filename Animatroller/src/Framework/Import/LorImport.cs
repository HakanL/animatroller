using System;
using System.IO;
using System.Collections.Generic;
using System.Xml.Serialization;
using System.Linq;
using System.Drawing;
using NLog;
using LMS = Animatroller.Framework.Import.FileFormat.LightORama.LMS;
using Animatroller.Framework.Extensions;
using Animatroller.Framework.Controller;

namespace Animatroller.Framework.Import
{
    public class LorImport : TimelineImporter
    {
        public class MappedDevice
        {
            public enum Components
            {
                Brightness,
                R,
                G,
                B
            }

            public LogicalDevice.IHasBrightnessControl BrightnessDevice { get; set; }
            public LogicalDevice.IHasColorControl ColorDevice { get; set; }
            public Components Component { get; set; }

            public void SetBrightness(double value)
            {
                switch (this.Component)
                {
                    case Components.Brightness:
                        this.BrightnessDevice.Brightness = value;
                        break;

                    case Components.R:
                        this.ColorDevice.Color = Color.FromArgb(
                            value.GetByteScale(),
                            this.ColorDevice.Color.G,
                            this.ColorDevice.Color.B);
                        break;

                    case Components.G:
                        this.ColorDevice.Color = Color.FromArgb(
                            this.ColorDevice.Color.R,
                            value.GetByteScale(),
                            this.ColorDevice.Color.B);
                        break;

                    case Components.B:
                        this.ColorDevice.Color = Color.FromArgb(
                            this.ColorDevice.Color.R,
                            this.ColorDevice.Color.G,
                            value.GetByteScale());
                        break;
                }
            }

            public void RunEffect(Effect.IMasterBrightnessEffect effect, TimeSpan oneSweepDuration)
            {
                //TODO
                this.BrightnessDevice.RunEffect(effect, oneSweepDuration);
            }


            public MappedDevice(ILogicalDevice device, Components component)
            {
                this.Component = component;
                if (device is LogicalDevice.IHasColorControl)
                    this.ColorDevice = (LogicalDevice.IHasColorControl)device;
                else if (device is LogicalDevice.IHasBrightnessControl)
                    this.BrightnessDevice = (LogicalDevice.IHasBrightnessControl)device;
                else
                    throw new ArgumentException();
            }
        }

        //protected Dictionary<Tuple<int, int>, HashSet<LogicalDevice.IHasBrightnessControl>> mappedDimmerDevices;
        //protected Dictionary<Tuple<int, int, int, int>, HashSet<LogicalDevice.IHasColorControl>> mappedRGBDevices;
        //protected Dictionary<UnitCircuit, HashSet<MappedDevice>> mappedDevices;
        protected LMS.sequence sequence;
        private Effect2.Shimmer shimmerEffect = new Effect2.Shimmer(0.5, 1.0);

        public LorImport(string filename)
        {
            //this.mappedDimmerDevices = new Dictionary<Tuple<int, int>, HashSet<LogicalDevice.IHasBrightnessControl>>();
            //this.mappedRGBDevices = new Dictionary<Tuple<int, int, int, int>, HashSet<LogicalDevice.IHasColorControl>>();
            //this.mappedDevices = new Dictionary<UnitCircuit, HashSet<MappedDevice>>();

            var deserializer = new XmlSerializer(typeof(LMS.sequence));

            using (TextReader textReader = new StreamReader(filename))
            {
                sequence = (LMS.sequence)deserializer.Deserialize(textReader);
            }

            foreach (var channel in sequence.channels)
            {
                var channelIdentity = new UnitCircuit(channel.unit, channel.circuit);
                this.channelData[channelIdentity] = new ChannelData(channel.name);
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

        // Light-O-Rama Musical Sequence
        public override Timeline CreateTimeline(bool loop)
        {
            var timeline = InternalCreateTimeline(loop);
//            timeline.MultiTimelineTrigger += timeline_MultiTimelineTrigger;

            foreach (var channel in this.sequence.channels)
            {
                log.Info("Channel [{0}]   Unit: {1}   Circuit: {2}", channel.name, channel.unit, channel.circuit);

                var mappedKey = new UnitCircuit(channel.unit, channel.circuit);
                HashSet<MappedDevice> mapDevices;
                //if (!mappedDevices.TryGetValue(mappedKey, out mapDevices))
                //{
                //    log.Warn("No devices mapped to unit {0}/circuit {1}, skipping", channel.unit, channel.circuit);
                //    continue;
                //}

                //HashSet<LogicalDevice.IHasBrightnessControl> devices;
                //var key = new Tuple<int, int>(channel.unit, channel.circuit);
                //if (!mappedDimmerDevices.TryGetValue(key, out devices))
                //{
                //    log.Warn("No devices mapped to unit {0}/circuit {1}, skipping", channel.unit, channel.circuit);
                //    continue;
                //}

                foreach (var effect in channel.effect)
                {
                    if (channel.deviceType != "LOR")
                        log.Warn("Not supporting device type {0} yet", channel.deviceType);

//                    var lorEvent = new LOREvent(mapDevices, effect);

//                    timeline.AddMs((int)(effect.startCentisecond * 10), lorEvent);
                }
            }

            return timeline;
        }

        private void timeline_MultiTimelineTrigger(object sender, Timeline<LOREvent>.MultiTimelineEventArgs e)
        {
            foreach (var lorEvent in e.Code)
            {
                foreach (var device in lorEvent.Devices)
                {
                    switch (lorEvent.type)
                    {
                        case "intensity":
                            if (string.IsNullOrEmpty(lorEvent.startIntensity))
                            {
                                device.SetBrightness((double)lorEvent.intensity / 100);
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


        public class UnitCircuit : IChannelIdentity
        {
            public int Unit { get; set; }
            public int Circuit { get; set; }

            public UnitCircuit(int unit, int circuit)
            {
                this.Unit = unit;
                this.Circuit = circuit;
            }

            public override bool Equals(object obj)
            {
                var b = (obj as UnitCircuit);
                
                return this.Unit == (obj as UnitCircuit).Unit &&
                    this.Circuit == (obj as UnitCircuit).Circuit;
            }

            public override int GetHashCode()
            {
                return this.Unit.GetHashCode() ^ this.Circuit.GetHashCode();
            }

            public override string ToString()
            {
                return string.Format("U{0}C{1}", this.Unit, this.Circuit);
            }
        }
    }

    public class LOREvent : LMS.channelsChannelEffect
    {
        public HashSet<LorImport.MappedDevice> Devices { get; private set; }

        public LOREvent(HashSet<LorImport.MappedDevice> devices, LMS.channelsChannelEffect effect)
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
}
