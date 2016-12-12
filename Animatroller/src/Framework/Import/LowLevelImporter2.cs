using System;
using System.Collections.Generic;
using System.Drawing;
using NLog;
using Animatroller.Framework.Controller;

namespace Animatroller.Framework.Import
{
    public abstract class LowLevelImporter2 : BaseImporter2
    {
        protected int effectsPerChannel;
        protected int eventPeriodInMilliseconds;

        public LowLevelImporter2(int priority = 1, [System.Runtime.CompilerServices.CallerMemberName] string name = "")
            : base(name, priority)
        {
        }

        /*        public override Controller.Timeline2<TChannelEffect> CreateTimeline(int? iterations)
                {
                    var timeline = InternalCreateTimeline(iterations);

                    foreach (var kvp in this.mappedDevices)
                    {
                        var channelIdentity = kvp.Key;

                        foreach(var effectData in channelEffectsPerChannel[channelIdentity])
                        {
        //                    timeline.AddMs();
                        }
        /*                byte? lastValue = null;
                        for (int i = 0; i < effectData.Length; i++)
                        {
                            if (effectData[i] == lastValue)
                                continue;
                            lastValue = effectData[i];

                            var timelineEvent = new SimpleDimmerEvent2(kvp.Value, (double)effectData[i] / 255);
                            timeline.AddMs(i * eventPeriodInMilliseconds, timelineEvent);
                        }*/
        /*}*/
        /*
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

                            var timelineEvent = new SimpleColorEvent2(kvp.Value, color);
                            timeline.AddMs(i * eventPeriodInMilliseconds, timelineEvent);

                            //log.Debug("Pos {0} set color {1} for device R:{2}/G:{3}/B:{4}", i, color, channelIdentity.R,
                            //    channelIdentity.G, channelIdentity.B);
                        }
                    }
        */
        //            return timeline;
        //      }
    }
    /*
        public class SimpleDimmerEvent2 : BaseImporter2.ISimpleInvokeEvent
        {
            private IEnumerable<BaseImporter2.MappedDeviceDimmer> devices;
            private double brightness;

            public SimpleDimmerEvent2(IEnumerable<BaseImporter2.MappedDeviceDimmer> devices, double brightness)
            {
                this.devices = devices;
                this.brightness = brightness;
            }

            public void Invoke()
            {
                foreach (var device in this.devices)
                {
    //FIXME                device.Device.Brightness = this.brightness;
                }
            }
        }
    *//*
        public class SimpleColorEvent2 : BaseImporter2.ISimpleInvokeEvent
        {
            private IEnumerable<BaseImporter2.MappedDeviceRGB> devices;
            private Color color;

            public SimpleColorEvent2(IEnumerable<BaseImporter2.MappedDeviceRGB> devices, Color color)
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
        }*/
}
