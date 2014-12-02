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
using System.Threading.Tasks;
using Animatroller.Framework.LogicalDevice;

namespace Animatroller.Framework.Import
{
    public class HighLevelImporter2 : BaseImporter2
    {
        protected Timeline2<ChannelEffectInstance> timeline;
        protected Dictionary<IChannelIdentity, IList<ChannelEffect>> channelEffectsPerChannel;
        private bool prepared;
        private Dictionary<IOwnedDevice, ControlledObserver<double>> deviceObservers;

        public HighLevelImporter2()
        {
            this.channelEffectsPerChannel = new Dictionary<IChannelIdentity, IList<ChannelEffect>>();
            this.timeline = new Timeline2<ChannelEffectInstance>(iterations: 1);
            this.deviceObservers = new Dictionary<IOwnedDevice, ControlledObserver<double>>();
        }

        protected void WireUpTimeline(Action exec)
        {
            foreach (var kvp in this.channelData)
            {
                if (!kvp.Value.Mapped)
                {
                    log.Warn("No devices mapped to {0}", kvp.Key);
                }
            }

            timeline.TearDown(() =>
            {
                foreach (var controlledDevice in this.controlledDevices)
                    controlledDevice.TurnOff();
            });

            timeline.MultiTimelineTrigger += (sender, e) =>
            {
                foreach (var controlledDevice in this.controlledDevices)
                    controlledDevice.Suspend();
                try
                {
                    exec();
                    //foreach (var invokeEvent in e.Code)
                    //    invokeEvent.Invoke();
                }
                finally
                {
                    foreach (var controlledDevice in this.controlledDevices)
                        controlledDevice.Resume();
                }
            };
        }

        protected void PopulateTimeline()
        {
            foreach (var kvp in this.channelData)
            {
                if (!kvp.Value.Mapped)
                {
                    log.Warn("No devices mapped to {0}", kvp.Key);
                }
            }

            foreach (var kvp in this.mappedDevices)
            {
                var channelIdentity = kvp.Key;

                foreach (var effectData in channelEffectsPerChannel[channelIdentity])
                {
                    var effectInstance = new ChannelEffectInstance
                    {
                        Devices = kvp.Value,
                        Effect = effectData
                    };
                    timeline.AddMs(effectData.StartMs, effectInstance);
                }
            }

            timeline.Setup(() =>
                {
                    foreach (var device in this.mappedDevices.SelectMany(x => x.Value))
                    {
                        if (!this.deviceObservers.ContainsKey(device))
                        {
                            var observer = device.GetBrightnessObserver();

                            this.deviceObservers.Add(device, observer);
                        }
                    }
                });

            timeline.TearDown(() =>
                {
                    foreach (var controlledDevice in this.controlledDevices)
                        controlledDevice.TurnOff();

                    // Release locks
                    foreach (var observer in this.deviceObservers.Values)
                        observer.Dispose();
                    this.deviceObservers.Clear();
                });

            timeline.MultiTimelineTrigger += (sender, e) =>
            {
                foreach (var controlledDevice in this.controlledDevices)
                    controlledDevice.Suspend();

                try
                {
                    foreach (var effectInstance in e.Code)
                    {
                        foreach (var device in effectInstance.Devices)
                        {
                            ControlledObserver<double> observer;
                            if (!this.deviceObservers.TryGetValue(device, out observer))
                                // Why no lock?
                                continue;

                            effectInstance.Effect.Execute(observer);
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
            foreach (var kvp in channelData)
            {
                log.Info("Channel {0} - {1}", kvp.Key, kvp.Value.Name);
            }
        }

        public void MapDevice(string channelName, IReceivesBrightness device)
        {
            var id = ChannelIdentityFromName(channelName);
            InternalMapDevice(id, device);
        }

        protected abstract class ChannelEffect
        {
            public int StartMs { get; set; }

            public abstract void Execute(IObserver<double> device);
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
            public IEnumerable<IReceivesBrightness> Devices { get; set; }

            public ChannelEffect Effect { get; set; }
        }

        public override Task Start()
        {
            Prepare();

            return this.timeline.Start();
        }

        public override void Stop()
        {
            this.timeline.Stop();
        }
    }
}
