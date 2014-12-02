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
    // Light-O-Rama Musical Sequence
    public class LorImport2 : HighLevelImporter2
    {
        public LorImport2()
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
                    log.Warn("Not supporting device type {0} yet", channel.deviceType);
                    continue;
                }

                var channelIdentity = new UnitCircuit(channel.unit, channel.circuit);
                AddChannelData(channelIdentity, new ChannelData(channel.name));

                var channelEffects = new List<ChannelEffect>();
                if (channel.effect != null)
                {
                    foreach (var effect in channel.effect)
                    {
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
                    return new List<ChannelEffect>() { new TwinkleChannelEffect
                    {
                        StartMs = startMs,
                        EndMs = endMs,
                        Brightness = double.Parse(effect.intensity) / 100.0
                    }};

                default:
                    throw new NotImplementedException("Unknown effect type " + effect.type);
            }
        }

        public void MapDevice(int unit, int circuit, IReceivesBrightness device)
        {
            InternalMapDevice(new UnitCircuit(unit, circuit), device);
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

        protected class InstantChannelEffect : ChannelEffect
        {
            public double Brightness { get; set; }

            public override void Execute(IObserver<double> device)
            {
                device.OnNext(Brightness);
            }
        }

        protected class FadeChannelEffect : ChannelEffectRange
        {
            public double StartBrightness { get; set; }

            public double EndBrightness { get; set; }

            public override void Execute(IObserver<double> device)
            {
                Executor.Current.MasterFader.Fade(device, StartBrightness, EndBrightness, DurationMs);
            }
        }

        protected class ShimmerChannelEffect : ChannelEffectRange
        {
            public override void Execute(IObserver<double> device)
            {
//                Executor.Current.MasterShimmer.Shimmer(device, 0, 1, DurationMs);
            }
        }

        protected class TwinkleChannelEffect : ChannelEffectRange
        {
            public double Brightness { get; set; }

            public override void Execute(IObserver<double> device)
            {
            }
        }
    }
}
