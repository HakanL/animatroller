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
    public class LorImport : BufferImporter
    {
        private Effect2.Shimmer shimmerEffect = new Effect2.Shimmer(0.5, 1.0);

        public LorImport(string filename)
        {
            var deserializer = new XmlSerializer(typeof(LMS.sequence));

            LMS.sequence sequence;
            using (TextReader textReader = new StreamReader(filename))
            {
                sequence = (LMS.sequence)deserializer.Deserialize(textReader);
            }

            this.eventPeriodInMilliseconds = 50;
            this.effectsPerChannel = (int)(sequence.channels.Max(x => x.centiseconds) * 10 / this.eventPeriodInMilliseconds);

            foreach (var channel in sequence.channels)
            {
                if (channel.deviceType != "LOR")
                {
                    log.Warn("Not supporting device type {0} yet", channel.deviceType);
                    continue;
                }

                var channelIdentity = new UnitCircuit(channel.unit, channel.circuit);
                AddChannelData(channelIdentity, new ChannelData(channel.name));

                var channelEffectData = new byte[this.effectsPerChannel];

                for (int pos = 0; pos < this.effectsPerChannel; pos++)
                {
                    long centiSeconds = pos * this.eventPeriodInMilliseconds / 10;
                    foreach (var effect in channel.effect)
                    {
                        if (effect.startCentisecond > centiSeconds)
                            break;

                        if (centiSeconds >= effect.startCentisecond && centiSeconds < effect.endCentisecond)
                        {
                            channelEffectData[pos] = (byte)(effect.intensity * 255 / 100);
                            break;
                        }
                    }
                }
                effectDataPerChannel[channelIdentity] = channelEffectData;
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

    public class LOREvent : LMS.channelsChannelEffect, BaseImporter.ISimpleInvokeEvent
    {
        private IEnumerable<BaseImporter.MappedDeviceDimmer> devices;

        public LOREvent(IEnumerable<BaseImporter.MappedDeviceDimmer> devices, LMS.channelsChannelEffect effect)
        {
            this.devices = devices;
            base.endCentisecond = effect.endCentisecond;
            base.endIntensity = effect.endIntensity;
            base.intensity = effect.intensity;
            base.startCentisecond = effect.startCentisecond;
            base.startIntensity = effect.startIntensity;
            base.type = effect.type;
        }

        public void Invoke()
        {
            foreach (var device in this.devices)
            {
                switch (this.type)
                {
                    case "intensity":
                        if (string.IsNullOrEmpty(this.startIntensity))
                        {
                            device.Device.Brightness = (double)this.intensity / 100;
                        }
                        else
                        {
                            device.RunEffect(
                                new Effect2.Fader(
                                    double.Parse(this.startIntensity) / 100,
                                    double.Parse(this.endIntensity) / 100),
                                TimeSpan.FromMilliseconds((this.endCentisecond - this.startCentisecond) * 100));
                        }
                        break;

                    case "shimmer":
                        //device.RunEffect(
                        //    shimmerEffect,
                        //    TimeSpan.FromMilliseconds((this.endCentisecond - this.startCentisecond) * 100));
                        break;

                    default:
                        //                        log.Warn("Unknown type {0}", lorEvent.type);
                        break;
                }
            }
        }
    }
}
