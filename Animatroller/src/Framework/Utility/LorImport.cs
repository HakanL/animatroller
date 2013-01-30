using System;
using System.IO;
using System.Collections.Generic;
using System.Xml.Serialization;
using NLog;
using LMS = Animatroller.Framework.Import.Schemas.LightORama.LMS;

namespace Animatroller.Framework.Utility
{
    public class LorImport
    {
        protected static Logger log = LogManager.GetCurrentClassLogger();
        protected Dictionary<Tuple<int, int>, LogicalDevice.IHasBrightnessControl> mappedDevices;

        public LorImport()
        {
            this.mappedDevices = new Dictionary<Tuple<int, int>, LogicalDevice.IHasBrightnessControl>();
        }

        public LorImport MapDevice(int circuit, int unit, LogicalDevice.IHasBrightnessControl device)
        {
            var key = new Tuple<int, int>(circuit, unit);
            mappedDevices[key] = device;

            return this;
        }

        // Light-O-Rama Musical Sequence
        public LorImport ImportLMSFile(string filename)
        {
            LMS.sequence sequence;

            var deserializer = new XmlSerializer(typeof(LMS.sequence));

            using (TextReader textReader = new StreamReader(filename))
            {
                sequence = (LMS.sequence)deserializer.Deserialize(textReader);
            }

            var timeline = new Timeline<LOREvent>();
            timeline.TimelineTrigger += timeline_TimelineTrigger;

            foreach (var channel in sequence.channels)
            {
                log.Info("Channel [{0}]   Circuit: {1}   Unit: {2}", channel.name, channel.circuit, channel.unit);

                LogicalDevice.IHasBrightnessControl device;
                var key = new Tuple<int, int>(channel.circuit, channel.unit);
                if (!mappedDevices.TryGetValue(key, out device))
                {
                    log.Warn("No device mapped to circuit {0}/unit {1}, skipping", channel.circuit, channel.unit);
                    continue;
                }

                foreach (var effect in channel.effect)
                {
                    var lorEvent = new LOREvent(device, effect);

                    timeline.Add((double)effect.startCentisecond / 10, lorEvent);
                }
            }

            timeline.Start();

            return this;
        }

        private void timeline_TimelineTrigger(object sender, Timeline<LOREvent>.TimelineEventArgs e)
        {
            var lorEvent = e.Code;

            log.Debug("Trigger!   {0}", lorEvent.type);

            switch (lorEvent.type)
            {
                case "intensity":
                    lorEvent.Device.Brightness = (double)lorEvent.intensity / 100;
                    break;
            }
        }
    }

    public class LOREvent : LMS.channelsChannelEffect
    {
        public LogicalDevice.IHasBrightnessControl Device { get; private set; }

        public LOREvent(LogicalDevice.IHasBrightnessControl device, LMS.channelsChannelEffect effect)
        {
            Device = device;
            base.endCentisecond = effect.endCentisecond;
            base.endIntensity = effect.endIntensity;
            base.intensity = effect.intensity;
            base.startCentisecond = effect.startCentisecond;
            base.startIntensity = effect.startIntensity;
            base.type = effect.type;
        }
    }
}
