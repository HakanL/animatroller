using System;
using System.IO;
using System.Collections.Generic;
using System.Xml.Serialization;
using System.Linq;
using System.Drawing;
using NLog;
using VIX = Animatroller.Framework.Import.FileFormat.Vixen;
using Animatroller.Framework.Extensions;
using Animatroller.Framework.Controller;

namespace Animatroller.Framework.Import
{
    public class VixenImport : MultiChannelTimeline
    {
        public class VixenChannel : IChannelIdentity
        {
            public int Channel { get; set; }

            public VixenChannel(int channel)
            {
                this.Channel = channel;
            }

            public override bool Equals(object obj)
            {
                var b = (obj as VixenChannel);
                return this.Channel == b.Channel;
            }

            public override int GetHashCode()
            {
                return this.Channel.GetHashCode();
            }

            public override string ToString()
            {
                return string.Format("{0}", this.Channel);
            }
        }

        protected static Logger log = LogManager.GetCurrentClassLogger();
        protected Dictionary<IChannelIdentity, byte[]> effectDataPerChannel;

        public VixenImport(string filename)
        {
            var deserializer = new XmlSerializer(typeof(VIX.Program));

            VIX.Program sequence;
            using (TextReader textReader = new StreamReader(filename))
            {
                sequence = (VIX.Program)deserializer.Deserialize(textReader);
            }

            this.eventPeriodInMilliseconds = sequence.EventPeriodInMilliseconds;

            byte[] effectData = Convert.FromBase64String(sequence.EventValues);
            this.effectsPerChannel = effectData.Length / sequence.Channels.Length;

            this.effectDataPerChannel = new Dictionary<IChannelIdentity, byte[]>();
            int i = 0;
            foreach (var channel in sequence.Channels)
            {
                var channelIdentity = new VixenChannel(channel.output);
                this.channelData[channelIdentity] = new ChannelData(channel.Value);

                var channelEffectData = new byte[this.effectsPerChannel];
                Array.Copy(effectData, i, channelEffectData, 0, channelEffectData.Length);
                effectDataPerChannel[channelIdentity] = channelEffectData;

                i += this.effectsPerChannel;
            }

        }

        public Timeline<IMultiChannelTimelineEvent> CreateTimeline(bool loop)
        {
            var timeline = base.CreateTimeline(loop);

            foreach (var kvp in this.mappedDevices)
            {
                var channelIdentity = kvp.Key;

                var effectData = effectDataPerChannel[channelIdentity];

                for (int i = 0; i < effectData.Length; i++)
                {
                    var vixEvent = new VixenEvent(kvp.Value, (double)effectData[i] / 255);
                    timeline.AddMs(i * eventPeriodInMilliseconds, vixEvent);
                }
            }

            foreach (var kvp in this.mappedRGBDevices)
            {
                var channelIdentity = kvp.Key;

                var effectDataR = effectDataPerChannel[channelIdentity.R];
                var effectDataG = effectDataPerChannel[channelIdentity.G];
                var effectDataB = effectDataPerChannel[channelIdentity.B];

                for (int i = 0; i < effectDataR.Length; i++)
                {
                    var color = Color.FromArgb(effectDataR[i], effectDataG[i], effectDataB[i]);

                    var vixEvent = new VixenEvent(kvp.Value, color);
                    timeline.AddMs(i * eventPeriodInMilliseconds, vixEvent);
                }
            }

            return timeline;
        }
    }

    public class VixenEvent : IMultiChannelTimelineEvent
    {
        public HashSet<VixenImport.MappedDeviceDimmer> Devices { get; private set; }
        public HashSet<VixenImport.MappedDeviceRGB> RGBDevices { get; private set; }
        public double Brightness { get; private set; }
        public Color Color { get; private set; }

        public VixenEvent(HashSet<VixenImport.MappedDeviceDimmer> devices,
            double brightness)
        {
            Devices = devices;
            Brightness = brightness;
        }

        public VixenEvent(HashSet<VixenImport.MappedDeviceRGB> rgbDevices,
            Color color)
        {
            RGBDevices = rgbDevices;
            Color = color;
        }

        public void Invoke()
        {
            if (Devices != null)
            {
                foreach (var device in Devices)
                {
                    device.Device.Brightness = Brightness;
                }
            }
            if (RGBDevices != null)
            {
                foreach (var device in RGBDevices)
                {
                    device.Device.Color = Color;
                }
            }
        }
    }
}
