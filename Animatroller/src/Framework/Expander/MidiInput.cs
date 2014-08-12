using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Animatroller.Framework.LogicalDevice;
using Animatroller.Framework.Extensions;
using NLog;
using Sanford.Multimedia.Midi;

namespace Animatroller.Framework.Expander
{
    public class MidiInput : IPort, IRunnable
    {
        protected static Logger log = LogManager.GetCurrentClassLogger();
        private InputDevice inputDevice;
        private Dictionary<Tuple<ChannelCommand, int>, Action<ChannelMessage>> messageMapper;

        public MidiInput()
        {
            if (InputDevice.DeviceCount == 0)
                throw new ArgumentException("No Midi device detected");

            this.messageMapper = new Dictionary<Tuple<ChannelCommand, int>, Action<ChannelMessage>>();

            this.inputDevice = new InputDevice(0);
            this.inputDevice.ChannelMessageReceived += inputDevice_ChannelMessageReceived;

            Executor.Current.Register(this);
        }

        private void inputDevice_ChannelMessageReceived(object sender, ChannelMessageEventArgs e)
        {
            log.Trace("Received midi command {0}, data1: {1}   data2: {2}",
                e.Message.Command,
                e.Message.Data1,
                e.Message.Data2);

            try
            {
                var key = Tuple.Create(e.Message.Command, e.Message.Data1);

                Action<ChannelMessage> action;
                if (this.messageMapper.TryGetValue(key, out action))
                {
                    action.Invoke(e.Message);
                }
            }
            catch (Exception ex)
            {
                log.Error("Failed to invoke action", ex);
            }
        }

        public void Start()
        {
            this.inputDevice.StartRecording();
        }

        public void Stop()
        {
            this.inputDevice.StopRecording();
            this.inputDevice.Close();
        }

        private void WireUpDevice_Note(Animatroller.Framework.PhysicalDevice.DigitalInput device, int note)
        {
            this.messageMapper.Add(Tuple.Create(ChannelCommand.NoteOn, note), m =>
                {
                    device.Trigger(true);
                });

            this.messageMapper.Add(Tuple.Create(ChannelCommand.NoteOff, note), m =>
                {
                    device.Trigger(false);
                });
        }

        public Animatroller.Framework.PhysicalDevice.DigitalInput AddDigitalInput_Note(DigitalInput logicalDevice, int note)
        {
            var device = new Animatroller.Framework.PhysicalDevice.DigitalInput();

            WireUpDevice_Note(device, note);

            device.Connect(logicalDevice);

            return device;
        }

        public Animatroller.Framework.PhysicalDevice.AnalogInput AddAnalogInput_Note(AnalogInput logicalDevice, int note)
        {
            var device = new Animatroller.Framework.PhysicalDevice.AnalogInput();

            this.messageMapper.Add(Tuple.Create(ChannelCommand.NoteOn, note), m =>
            {
                device.Trigger(m.Data2 / 127.0);
            });

            this.messageMapper.Add(Tuple.Create(ChannelCommand.NoteOff, note), m =>
            {
                device.Trigger(0);
            });

            device.Connect(logicalDevice);

            return device;
        }

        public Animatroller.Framework.PhysicalDevice.AnalogInput AddAnalogInput_Controller(AnalogInput logicalDevice, int controller)
        {
            var device = new Animatroller.Framework.PhysicalDevice.AnalogInput();

            this.messageMapper.Add(Tuple.Create(ChannelCommand.Controller, controller), m =>
            {
                device.Trigger(m.Data2 / 127.0);
            });

            device.Connect(logicalDevice);

            return device;
        }
    }
}
