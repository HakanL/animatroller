using System;
using System.IO;
using System.Collections.Generic;
using System.Xml.Serialization;
using System.Linq;
using System.Drawing;
using NLog;
using VIX = Animatroller.Framework.Import.Schemas.Vixen;
using Animatroller.Framework.Extensions;

namespace Animatroller.Framework.Utility
{
    public class VixenImport
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

            public override bool Equals(object obj)
            {
                return this.Unit == (obj as UnitCircuit).Unit &&
                    this.Circuit == (obj as UnitCircuit).Circuit;
            }

            public override int GetHashCode()
            {
                return this.Unit.GetHashCode() ^ this.Circuit.GetHashCode();
            }
        }

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

        protected static Logger log = LogManager.GetCurrentClassLogger();
        protected Dictionary<Tuple<int, int>, HashSet<LogicalDevice.IHasBrightnessControl>> mappedDimmerDevices;
        protected Dictionary<Tuple<int, int, int, int>, HashSet<LogicalDevice.IHasColorControl>> mappedRGBDevices;
        protected Dictionary<UnitCircuit, HashSet<MappedDevice>> mappedDevices;
        protected VIX.Program sequence;
        private Effect2.Shimmer shimmerEffect = new Effect2.Shimmer(0.5, 1.0);

        public VixenImport(string filename)
        {
            this.mappedDimmerDevices = new Dictionary<Tuple<int, int>, HashSet<LogicalDevice.IHasBrightnessControl>>();
            this.mappedRGBDevices = new Dictionary<Tuple<int, int, int, int>, HashSet<LogicalDevice.IHasColorControl>>();
            this.mappedDevices = new Dictionary<UnitCircuit, HashSet<MappedDevice>>();

            var deserializer = new XmlSerializer(typeof(VIX.Program));

            using (TextReader textReader = new StreamReader(filename))
            {
                sequence = (VIX.Program)deserializer.Deserialize(textReader);
            }
        }

        public IEnumerable<int> AvailableUnits
        {
            get
            {
                //FIXME
                return new List<int>() { 1 };
            }
        }

        public IEnumerable<int> GetCircuits(int unit)
        {
            var list = new HashSet<int>();

            foreach (var channel in sequence.Channels)
            {
//                if (channel.unit == unit)
//                    list.Add(channel.circuit);
            }

            return list.OrderBy(x => x);
        }

        public IEnumerable<UnitCircuit> AvailableChannels
        {
            get
            {
                var list = new HashSet<Tuple<int, int>>();

                foreach (var channel in sequence.Channels)
                    list.Add(new Tuple<int, int>(1, channel.output));

                return list.Select(x => new UnitCircuit(x.Item1, x.Item2));
            }
        }

        public string GetChannelName(int unit, int circuit)
        {
            foreach (var channel in sequence.Channels)
            {
                if (channel.output == circuit)
                    return channel.Value;
            }

            throw new ArgumentOutOfRangeException("Circuit/Unit does not exist");
        }

        public System.Drawing.Color GetChannelColor(int unit, int circuit)
        {
            foreach (var channel in sequence.Channels)
            {
                if (channel.output == circuit)
                    return Color.FromArgb(int.Parse(channel.color));
            }

            throw new ArgumentOutOfRangeException("Circuit/Unit does not exist");
        }

        protected void InternalMapDevice(int unit, int circuit, MappedDevice device)
        {
            var key = new UnitCircuit(unit, circuit);
            HashSet<MappedDevice> devices;
            if (!mappedDevices.TryGetValue(key, out devices))
            {
                devices = new HashSet<MappedDevice>();
                mappedDevices[key] = devices;
            }
            devices.Add(device);
        }

        public VixenImport MapDevice(int unit, int circuit, LogicalDevice.IHasBrightnessControl device)
        {
            var key = new Tuple<int, int>(unit, circuit);
            HashSet<LogicalDevice.IHasBrightnessControl> devices;
            if (!mappedDimmerDevices.TryGetValue(key, out devices))
            {
                devices = new HashSet<LogicalDevice.IHasBrightnessControl>();
                mappedDimmerDevices[key] = devices;
            }
            devices.Add(device);

            device.Brightness = 0.0;

            var mappedDevice = new MappedDevice(device, MappedDevice.Components.Brightness);
            InternalMapDevice(unit, circuit, mappedDevice);

            return this;
        }

        public T MapDevice<T>(int unit, int circuit, Func<string, T> logicalDevice) where T : LogicalDevice.IHasBrightnessControl
        {
            string name = GetChannelName(unit, circuit);

            var device = logicalDevice.Invoke(name);

            MapDevice(unit, circuit, device);

            return device;
        }

        public VixenImport MapDevice(int unit, int circuitR, int circuitG, int circuitB, LogicalDevice.IHasColorControl device)
        {
            var key = new Tuple<int, int, int, int>(unit, circuitR, circuitG, circuitB);
            HashSet<LogicalDevice.IHasColorControl> devices;
            if (!mappedRGBDevices.TryGetValue(key, out devices))
            {
                devices = new HashSet<LogicalDevice.IHasColorControl>();
                mappedRGBDevices[key] = devices;
            }
            devices.Add(device);
            device.Color = Color.Black;

            var mappedDevice = new MappedDevice(device, MappedDevice.Components.R);
            InternalMapDevice(unit, circuitR, mappedDevice);

            mappedDevice = new MappedDevice(device, MappedDevice.Components.G);
            InternalMapDevice(unit, circuitG, mappedDevice);

            mappedDevice = new MappedDevice(device, MappedDevice.Components.B);
            InternalMapDevice(unit, circuitB, mappedDevice);

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
        public VixenTimeline CreateTimeline(bool loop)
        {
            var timeline = new VixenTimeline(loop);
            timeline.MultiTimelineTrigger += timeline_MultiTimelineTrigger;

            foreach (var channel in this.sequence.Channels)
            {
                log.Info("Channel [{0}]   Circuit: {1}", channel.Value, channel.output);

                var mappedKey = new UnitCircuit(1, channel.output);
                HashSet<MappedDevice> mapDevices;
                if (!mappedDevices.TryGetValue(mappedKey, out mapDevices))
                {
                    log.Warn("No devices mapped to unit {0}/circuit {1}, skipping", 1, channel.output);
                    continue;
                }

                //HashSet<LogicalDevice.IHasBrightnessControl> devices;
                //var key = new Tuple<int, int>(channel.unit, channel.circuit);
                //if (!mappedDimmerDevices.TryGetValue(key, out devices))
                //{
                //    log.Warn("No devices mapped to unit {0}/circuit {1}, skipping", channel.unit, channel.circuit);
                //    continue;
                //}

                byte[] effectData = Convert.FromBase64String(sequence.EventValues);
                //foreach (var effect in channel.effect)
                //{
                //    if (channel.deviceType != "LOR")
                //        log.Warn("Not supporting device type {0} yet", channel.deviceType);

                //    var vixenEvent = new VixenEvent(mapDevices, effect);

                //    timeline.AddMs((int)(effect.startCentisecond * 10), vixenEvent);
                //}
            }

            return timeline;
        }

        private void timeline_MultiTimelineTrigger(object sender, Timeline<VixenEvent>.MultiTimelineEventArgs e)
        {
            foreach (var vixenEvent in e.Code)
            {
                foreach (var device in vixenEvent.Devices)
                {
//                    device.SetBrightness((double)vixenEvent.intensity / 100);
                }
            }
        }
    }

    public class VixenEvent// : VIX.channelsChannelEffect
    {
        public HashSet<VixenImport.MappedDevice> Devices { get; private set; }

        public VixenEvent(HashSet<VixenImport.MappedDevice> devices)//, VIX.channelsChannelEffect effect)
        {
            Devices = devices;
            //base.endCentisecond = effect.endCentisecond;
            //base.endIntensity = effect.endIntensity;
            //base.intensity = effect.intensity;
            //base.startCentisecond = effect.startCentisecond;
            //base.startIntensity = effect.startIntensity;
            //base.type = effect.type;
        }
    }

    public class VixenTimeline : Timeline<VixenEvent>
    {
        public VixenTimeline(bool loop)
            : base(loop)
        {
        }
    }
}
