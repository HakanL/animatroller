using System;
using System.IO;
using System.Collections.Generic;
using System.Xml.Serialization;
using System.Linq;
using System.Drawing;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using NLog;
using LMS = Animatroller.Framework.Import.FileFormat.LightORama.LMS;
using Animatroller.Framework.Extensions;
using Animatroller.Framework.Controller;
using System.Threading.Tasks;
using Animatroller.Framework.LogicalDevice;

namespace Animatroller.Framework.Import
{
    public class HighLevelImporter2 : BaseImporter2
    {
        protected Timeline2<ChannelEffectInstance> timeline;
        protected Dictionary<IChannelIdentity, IList<ChannelEffect>> channelEffectsPerChannel;
        private bool prepared;
        protected List<DeviceController> devices;
        //private Dictionary<IOwnedDevice, IDisposableObserver<double>> brightnessObservers;
        //private Dictionary<IOwnedDevice, ControlledObserverRGB> rgbObservers;
        private int lastProgressReport;
        private Subject<int> progress;
        protected object lockObject = new object();
        private GroupControlToken token;

        public HighLevelImporter2(string name, int priority)
            : base(name, priority)
        {
            this.channelEffectsPerChannel = new Dictionary<IChannelIdentity, IList<ChannelEffect>>();
            this.timeline = new Timeline2<ChannelEffectInstance>(iterations: 1);
            //this.brightnessObservers = new Dictionary<IOwnedDevice, IDisposableObserver<double>>();
            //this.rgbObservers = new Dictionary<IOwnedDevice, ControlledObserverRGB>();
            this.devices = new List<DeviceController>();

            this.progress = new Subject<int>();
        }

        public IObservable<int> Progress
        {
            get { return this.progress.AsObservable(); }
        }

        private void AddEffectData(IChannelIdentity channelIdentity, IEnumerable<DeviceController> devices, ChannelEffectInstance.DeviceType deviceType)
        {
            foreach (var effectData in this.channelEffectsPerChannel[channelIdentity])
            {
                var effectInstance = new ChannelEffectInstance
                {
                    Devices = devices,
                    Effect = effectData,
                    Type = deviceType
                };

                timeline.AddMs(effectData.StartMs, effectInstance);
            }
        }

        public IControlToken Token { get { return this.token; } }

        public void ListUnmappedChannels()
        {
            foreach (var kvp in this.channelData)
            {
                if (!kvp.Value.Mapped && kvp.Value.HasEffects)
                {
                    log.Warn("No devices mapped to {0} ({1})", kvp.Key, kvp.Value.Name);
                }
            }
        }

        protected void PopulateTimeline()
        {
            foreach (var kvp in this.mappedDevices)
            {
                AddEffectData(kvp.Key, kvp.Value, ChannelEffectInstance.DeviceType.Brightness);
            }

            foreach (var kvp in this.mappedRgbDevices)
            {
                var id = kvp.Key;

                AddEffectData(id.R, kvp.Value, ChannelEffectInstance.DeviceType.ColorR);
                AddEffectData(id.G, kvp.Value, ChannelEffectInstance.DeviceType.ColorG);
                AddEffectData(id.B, kvp.Value, ChannelEffectInstance.DeviceType.ColorB);
            }

            timeline.Setup(() =>
                {
                    if (this.token == null)
                    {
                        this.token = new GroupControlToken(this.devices.Select(x => x.Device), null, this.name, this.priority);

                        foreach (var device in this.devices)
                        {
                            device.Observer = device.Device.GetDataObserver(this.token);

                            device.Observer.SetDataFromIData(device.AdditionalData);
                        }
                    }
                    //foreach (var device in this.mappedDevices.SelectMany(x => x.Value))
                    //{
                    //    if (!this.brightnessObservers.ContainsKey(device))
                    //    {
                    //        var observer = device.GetBrightnessObserver();

                    //        this.brightnessObservers.Add(device, observer);
                    //    }
                    //}

                    //foreach (var device in this.mappedRgbDevices.SelectMany(x => x.Value))
                    //{
                    //    if (!this.rgbObservers.ContainsKey(device))
                    //    {
                    //        var observer = device.GetRgbObserver();

                    //        this.rgbObservers.Add(device, observer);
                    //    }
                    //}
                });

            timeline.TearDown(() =>
                {
                    foreach (var device in this.devices)
                    {
                        device.Observer = null;
                    }

                    if (this.token != null)
                    {
                        this.token.Dispose();
                        this.token = null;
                    }
                    //foreach (var controlledDevice in this.controlledDevices)
                    //    controlledDevice.TurnOff();

                    //// Release locks
                    //foreach (var observer in this.brightnessObservers.Values)
                    //    observer.Dispose();
                    //this.brightnessObservers.Clear();

                    //foreach (var observer in this.rgbObservers.Values)
                    //    observer.Dispose();
                    //this.rgbObservers.Clear();
                });

            timeline.MultiTimelineTrigger += (sender, e) =>
            {
                if ((e.ElapsedMs - this.lastProgressReport) > 1000)
                {
                    // Update progress
                    this.lastProgressReport = e.ElapsedMs;

                    this.progress.OnNext(e.ElapsedMs);
                }

                foreach (var controlledDevice in this.controlledDevices)
                    controlledDevice.Suspend();

                try
                {
                    foreach (var effectInstance in e.Code)
                    {
                        if (effectInstance.Type == ChannelEffectInstance.DeviceType.Brightness)
                        {
                            foreach (var device in effectInstance.Devices)
                            {
                                var receivesBrightness = device.Device as IReceivesBrightness;
                                if (receivesBrightness != null)
                                    effectInstance.Effect.Execute(receivesBrightness, this.token);
                            }
                        }
                        else if (effectInstance.Type == ChannelEffectInstance.DeviceType.ColorR)
                        {
                            foreach (var device in effectInstance.Devices)
                            {
                                var receivesColor = device.Device as IReceivesColor;
                                if (receivesColor != null)
                                    effectInstance.Effect.Execute(receivesColor, effectInstance.Type, this.token);
                            }
                        }
                        else if (effectInstance.Type == ChannelEffectInstance.DeviceType.ColorG)
                        {
                            foreach (var device in effectInstance.Devices)
                            {
                                var receivesColor = device.Device as IReceivesColor;
                                if (receivesColor != null)
                                    effectInstance.Effect.Execute(receivesColor, effectInstance.Type, this.token);
                            }
                        }
                        else if (effectInstance.Type == ChannelEffectInstance.DeviceType.ColorB)
                        {
                            foreach (var device in effectInstance.Devices)
                            {
                                var receivesColor = device.Device as IReceivesColor;
                                if (receivesColor != null)
                                    effectInstance.Effect.Execute(receivesColor, effectInstance.Type, this.token);
                            }
                        }
                    }
                }
                finally
                {
                    foreach (var controlledDevice in this.controlledDevices)
                        controlledDevice.Resume();
                }
            };
        }

        public void Prepare()
        {
            if (this.prepared)
                return;

            this.prepared = true;

            PopulateTimeline();
        }

        public void Dump()
        {
            log.Info("Used channels:");

            int count = 0;
            foreach (var kvp in this.channelData.Where(x => x.Value.HasEffects).OrderBy(x => x.Key))
            {
                count++;

                log.Info("Channel {0} - {1}", kvp.Key, kvp.Value.Name);
            }

            log.Info("Total used channels: {0}", count);
        }

        protected DeviceController ConnectTo(IReceivesBrightness device, params Tuple<DataElements, object>[] additionalData)
        {
            lock (this.lockObject)
            {
                IData data = null;
                if (additionalData.Any())
                {
                    data = new Data();
                    foreach (var kvp in additionalData)
                        data[kvp.Item1] = kvp.Item2;
                }

                var deviceController = new DeviceController(device, data);
                this.devices.Add(deviceController);

                return deviceController;
            }
        }

        protected DeviceController ConnectTo(IReceivesColor device, params Tuple<DataElements, object>[] additionalData)
        {
            lock (this.lockObject)
            {
                IData data = null;
                if (additionalData.Any())
                {
                    data = new Data();
                    foreach (var kvp in additionalData)
                        data[kvp.Item1] = kvp.Item2;
                }

                var deviceController = new DeviceController(device, data);
                this.devices.Add(deviceController);

                return deviceController;
            }
        }

        public void ControlDevice(IReceivesBrightness device)
        {
            ConnectTo(device);
        }

        public void MapDevice(string channelName, IReceivesBrightness device, params Tuple<DataElements, object>[] additionalData)
        {
            var id = ChannelIdentityFromName(channelName);

            var deviceController = ConnectTo(device, additionalData);

            InternalMapDevice(id, deviceController);
        }

        public void MapDeviceRGB(string channelNameR, string channelNameG, string channelNameB, IReceivesColor device)
        {
            var id = new RGBChannelIdentity(
                ChannelIdentityFromName(channelNameR),
                ChannelIdentityFromName(channelNameG),
                ChannelIdentityFromName(channelNameB));

            var deviceController = ConnectTo(device);

            InternalMapDevice(id, deviceController);
        }

        public void MapDeviceRGBW(string channelNameR, string channelNameG, string channelNameB, string channelNameW, IReceivesColor device)
        {
            // Currently not used
            var idW = ChannelIdentityFromName(channelNameW);

            var id = new RGBChannelIdentity(
                ChannelIdentityFromName(channelNameR),
                ChannelIdentityFromName(channelNameG),
                ChannelIdentityFromName(channelNameB));

            var deviceController = ConnectTo(device);

            InternalMapDevice(id, deviceController);
        }

        protected abstract class ChannelEffect
        {
            public int StartMs { get; set; }

            public abstract void Execute(IReceivesBrightness device, IControlToken token);

            public abstract void Execute(IReceivesColor device, ChannelEffectInstance.DeviceType deviceType, IControlToken token);
        }

        protected abstract class ChannelEffectRange : ChannelEffect
        {
            public int EndMs { get; set; }

            public int DurationMs
            {
                get { return EndMs - StartMs; }
            }
        }

        protected class ChannelEffectInstance
        {
            public enum DeviceType
            {
                Brightness,
                ColorR,
                ColorG,
                ColorB
            }

            public IEnumerable<DeviceController> Devices { get; set; }

            public ChannelEffect Effect { get; set; }

            public DeviceType Type { get; set; }
        }

        public override Task Start(long offsetMs = 0, TimeSpan? duration = null)
        {
            Prepare();

            // Make sure we're stopped
            Stop();

            return this.timeline.Start(offsetMs, duration);
        }

        public override void Stop()
        {
            this.timeline.Stop();
            this.lastProgressReport = 0;
        }
    }
}
