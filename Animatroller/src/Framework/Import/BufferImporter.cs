using System;
using System.Collections.Generic;
using System.Drawing;
using Serilog;
using Animatroller.Framework.Controller;

namespace Animatroller.Framework.Import
{
    public abstract class BufferImporter : BaseImporter
    {
        protected Dictionary<IChannelIdentity, byte[]> effectDataPerChannel;
        protected int effectsPerChannel;
        protected int eventPeriodInMilliseconds;

        public BufferImporter()
        {
            this.effectDataPerChannel = new Dictionary<IChannelIdentity, byte[]>();
        }

        public override Timeline CreateTimeline(int? iterations)
        {
            var timeline = InternalCreateTimeline(iterations);

            foreach (var kvp in this.mappedDevices)
            {
                var channelIdentity = kvp.Key;

                var effectData = effectDataPerChannel[channelIdentity];

                byte? lastValue = null;
                for (int i = 0; i < effectData.Length; i++)
                {
                    if (effectData[i] == lastValue)
                        continue;
                    lastValue = effectData[i];

                    var timelineEvent = new SimpleDimmerEvent(kvp.Value, (double)effectData[i] / 255);
                    timeline.AddMs(i * eventPeriodInMilliseconds, timelineEvent);
                }
            }

            foreach (var kvp in this.mappedRGBDevices)
            {
                var channelIdentity = kvp.Key;

                var effectDataR = effectDataPerChannel[channelIdentity.R];
                var effectDataG = effectDataPerChannel[channelIdentity.G];
                var effectDataB = effectDataPerChannel[channelIdentity.B];

                Color? lastValue = null;
                for (int i = 0; i < effectDataR.Length; i++)
                {
                    var color = Color.FromArgb(effectDataR[i], effectDataG[i], effectDataB[i]);
                    if (color == lastValue)
                        continue;
                    lastValue = color;

                    var timelineEvent = new SimpleColorEvent(kvp.Value, color);
                    timeline.AddMs(i * eventPeriodInMilliseconds, timelineEvent);

                    //log.Debug("Pos {0} set color {1} for device R:{2}/G:{3}/B:{4}", i, color, channelIdentity.R,
                    //    channelIdentity.G, channelIdentity.B);
                }
            }

            return timeline;
        }
    }

    public class SimpleDimmerEvent : BaseImporter.ISimpleInvokeEvent
    {
        private IEnumerable<BaseImporter.MappedDeviceDimmer> devices;
        private double brightness;

        public SimpleDimmerEvent(IEnumerable<BaseImporter.MappedDeviceDimmer> devices, double brightness)
        {
            this.devices = devices;
            this.brightness = brightness;
        }

        public void Invoke()
        {
            foreach (var device in this.devices)
            {
                device.Device.Brightness = this.brightness;
            }
        }
    }

    public class SimpleColorEvent : BaseImporter.ISimpleInvokeEvent
    {
        private IEnumerable<BaseImporter.MappedDeviceRGB> devices;
        private Color color;

        public SimpleColorEvent(IEnumerable<BaseImporter.MappedDeviceRGB> devices, Color color)
        {
            this.devices = devices;
            this.color = color;
        }

        public void Invoke()
        {
            foreach (var device in this.devices)
            {
                device.Device.Color = this.color;
            }
        }
    }
}
