using System;
using System.IO;
using System.Collections.Generic;
using System.Xml.Serialization;
using System.Linq;
using System.Drawing;
using Serilog;
using LMS = Animatroller.Framework.Import.FileFormat.LightORama.LMS;
using Animatroller.Framework.Extensions;
using Animatroller.Framework.Controller;

namespace Animatroller.Framework.Import
{
    // Light-O-Rama Musical Sequence
    public class LorImport2 : HighLevelImporter2
    {
        public LorImport2(int priority = 1, [System.Runtime.CompilerServices.CallerMemberName] string name = "")
            : base(name: name, priority: priority)
        {
        }

        public void LoadFromFile(string filename)
        {
            var deserializer = new XmlSerializer(typeof(LMS.sequence));

            LMS.sequence sequence;
            using (TextReader textReader = new StreamReader(filename))
            {
                sequence = (LMS.sequence)deserializer.Deserialize(textReader);
            }

            foreach (var channel in sequence.channels)
            {
                if (channel.deviceType != null && channel.deviceType != "LOR")
                {
                    this.log.Warning("Not supporting device type {0} yet", channel.deviceType);
                    continue;
                }

                if (channel.unit == 0 && channel.circuit == 0)
                    continue;

                var channelIdentity = new UnitCircuit(channel.unit, channel.circuit, channel.savedIndex);
                var channelData = new ChannelData(channel.name);

                AddChannelData(channelIdentity, channelData);

                var channelEffects = new List<ChannelEffect>();
                if (channel.effect != null)
                {
                    foreach (var effect in channel.effect)
                    {
                        channelData.HasEffects = true;

                        channelEffects.AddRange(GetChannelEffects(effect));
                    }
                }

                channelEffectsPerChannel[channelIdentity] = channelEffects;
            }
        }

        private IEnumerable<ChannelEffect> GetChannelEffects(LMS.channelsChannelEffect effect)
        {
            int startMs = effect.startCentisecond * 10;
            int endMs = effect.endCentisecond * 10;

            switch (effect.type)
            {
                case "intensity":
                    if (!string.IsNullOrEmpty(effect.intensity))
                    {
                        return new List<ChannelEffect>() {
                            new InstantChannelEffect
                            {
                                StartMs = startMs,
                                Brightness = double.Parse(effect.intensity) / 100.0
                            },
                            new InstantChannelEffect
                            {
                                StartMs = endMs,
                                Brightness = 0
                            }};
                    }
                    else
                    {
                        return new List<ChannelEffect>() { new FadeChannelEffect
                            {
                                StartMs = startMs,
                                EndMs = endMs,
                                StartBrightness = double.Parse(effect.startIntensity) / 100.0,
                                EndBrightness = double.Parse(effect.endIntensity) / 100.0
                            }};
                    }

                case "shimmer":
                    return new List<ChannelEffect>() { new ShimmerChannelEffect
                    {
                        StartMs = startMs,
                        EndMs = endMs
                    }};

                case "twinkle":
                    if (!string.IsNullOrEmpty(effect.intensity))
                        return new List<ChannelEffect>() { new TwinkleChannelEffect
                        {
                            StartMs = startMs,
                            EndMs = endMs,
                            StartBrightness = double.Parse(effect.intensity) / 100.0,
                            EndBrightness = double.Parse(effect.intensity) / 100.0
                        }};
                    else
                        return new List<ChannelEffect>() { new TwinkleChannelEffect
                        {
                            StartMs = startMs,
                            EndMs = endMs,
                            StartBrightness = double.Parse(effect.startIntensity) / 100.0,
                            EndBrightness = double.Parse(effect.endIntensity) / 100.0
                        }};

                default:
                    throw new NotImplementedException("Unknown effect type " + effect.type);
            }
        }

        //public void MapDevice(int unit, int circuit, int position, IReceivesBrightness device)
        //{
        //    InternalMapDevice(new UnitCircuit(unit, circuit, position), device);
        //}

        public class UnitCircuit : IChannelIdentity
        {
            public int Unit { get; set; }

            public int Circuit { get; set; }

            public int Position { get; set; }

            public UnitCircuit(int unit, int circuit, int position)
            {
                this.Unit = unit;
                this.Circuit = circuit;
                this.Position = position;
            }

            public override bool Equals(object obj)
            {
                var b = (obj as UnitCircuit);

                return this.Unit == (obj as UnitCircuit).Unit &&
                    this.Circuit == (obj as UnitCircuit).Circuit &&
                    this.Position == (obj as UnitCircuit).Position;
            }

            public override int GetHashCode()
            {
                return this.Unit.GetHashCode() ^ this.Circuit.GetHashCode() ^ this.Position.GetHashCode();
            }

            public override string ToString()
            {
                return string.Format("U{0}C{1}x{2}", this.Unit, this.Circuit, this.Position);
            }

            public int CompareTo(object obj)
            {
                var other = (UnitCircuit)obj;

                return this.Position.CompareTo(other.Position);
            }
        }

        protected class InstantChannelEffect : ChannelEffect
        {
            public double Brightness { get; set; }

            public override void Execute(IReceivesBrightness device, IControlToken token)
            {
                device.SetBrightness(Brightness, channel: 0, token: token);
            }

            public override void Execute(IReceivesColor device, ChannelEffectInstance.DeviceType deviceType, IControlToken token)
            {
                var currentColor = device.GetCurrentColor();
                switch (deviceType)
                {
                    case ChannelEffectInstance.DeviceType.ColorR:
                        device.SetColor(Color.FromArgb((int)(Brightness * 255), currentColor.G, currentColor.B), 1, token);
                        break;

                    case ChannelEffectInstance.DeviceType.ColorG:
                        device.SetColor(Color.FromArgb(currentColor.R, (int)(Brightness * 255), currentColor.B), 1, token);
                        break;

                    case ChannelEffectInstance.DeviceType.ColorB:
                        device.SetColor(Color.FromArgb(currentColor.R, currentColor.G, (int)(Brightness * 255)), 1, token);
                        break;
                }
            }
        }

        protected class FadeChannelEffect : ChannelEffectRange
        {
            public double StartBrightness { get; set; }

            public double EndBrightness { get; set; }

            public override void Execute(IReceivesBrightness device, IControlToken token)
            {
                Executor.Current.MasterEffect.Fade(device, StartBrightness, EndBrightness, DurationMs, token: token);
            }

            public override void Execute(IReceivesColor device, ChannelEffectInstance.DeviceType deviceType, IControlToken token)
            {
                Executor.Current.MasterEffect.Fade(new LogicalDevice.VirtualDevice(b =>
                {
                    var currentColor = device.GetCurrentColor();
                    switch (deviceType)
                    {
                        case ChannelEffectInstance.DeviceType.ColorR:
                            device.SetColor(Color.FromArgb((int)(b * 255), currentColor.G, currentColor.B), 1, token);
                            break;

                        case ChannelEffectInstance.DeviceType.ColorG:
                            device.SetColor(Color.FromArgb(currentColor.R, (int)(b * 255), currentColor.B), 1, token);
                            break;

                        case ChannelEffectInstance.DeviceType.ColorB:
                            device.SetColor(Color.FromArgb(currentColor.R, currentColor.G, (int)(b * 255)), 1, token);
                            break;
                    }
                }), StartBrightness, EndBrightness, DurationMs, token: token);
            }
        }

        protected class ShimmerChannelEffect : ChannelEffectRange
        {
            public override void Execute(IReceivesBrightness device, IControlToken token)
            {
                Executor.Current.MasterEffect.Shimmer(device, 0, 1, DurationMs, token: token);
            }

            public override void Execute(IReceivesColor device, ChannelEffectInstance.DeviceType deviceType, IControlToken token)
            {
                Executor.Current.MasterEffect.Shimmer(new LogicalDevice.VirtualDevice(b =>
                {
                    var currentColor = device.GetCurrentColor();
                    switch (deviceType)
                    {
                        case ChannelEffectInstance.DeviceType.ColorR:
                            device.SetColor(Color.FromArgb((int)(b * 255), currentColor.G, currentColor.B), 1, token);
                            break;

                        case ChannelEffectInstance.DeviceType.ColorG:
                            device.SetColor(Color.FromArgb(currentColor.R, (int)(b * 255), currentColor.B), 1, token);
                            break;

                        case ChannelEffectInstance.DeviceType.ColorB:
                            device.SetColor(Color.FromArgb(currentColor.R, currentColor.G, (int)(b * 255)), 1, token);
                            break;
                    }
                }), 0, 1, DurationMs, token: token);
            }
        }

        protected class TwinkleChannelEffect : ChannelEffectRange
        {
            public double StartBrightness { get; set; }

            public double EndBrightness { get; set; }

            public override void Execute(IReceivesBrightness device, IControlToken token)
            {
                Executor.Current.MasterEffect.Shimmer(device, StartBrightness, EndBrightness, DurationMs, token: token);
            }

            public override void Execute(IReceivesColor device, ChannelEffectInstance.DeviceType deviceType, IControlToken token)
            {
                Executor.Current.MasterEffect.Shimmer(new LogicalDevice.VirtualDevice(b =>
                {
                    var currentColor = device.GetCurrentColor();
                    switch (deviceType)
                    {
                        case ChannelEffectInstance.DeviceType.ColorR:
                            device.SetColor(Color.FromArgb((int)(b * 255), currentColor.G, currentColor.B), 1, token);
                            break;

                        case ChannelEffectInstance.DeviceType.ColorG:
                            device.SetColor(Color.FromArgb(currentColor.R, (int)(b * 255), currentColor.B), 1, token);
                            break;

                        case ChannelEffectInstance.DeviceType.ColorB:
                            device.SetColor(Color.FromArgb(currentColor.R, currentColor.G, (int)(b * 255)), 1, token);
                            break;
                    }
                }), StartBrightness, EndBrightness, DurationMs, token: token);
            }
        }
    }
}
