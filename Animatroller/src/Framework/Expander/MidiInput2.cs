using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Animatroller.Framework.LogicalDevice;
using Animatroller.Framework.Extensions;
using NLog;
using Sanford.Multimedia.Midi;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Concurrency;

namespace Animatroller.Framework.Expander
{
    public class MidiInput2 : IPort, IRunnable
    {
        protected static Logger log = LogManager.GetCurrentClassLogger();
        private InputDevice inputDevice;
        private Dictionary<Tuple<int, ChannelCommand, int>, Action<ChannelMessage>> messageMapper;
        private ISubject<ChannelMessage> midiMessages;
        private string name;

        public MidiInput2(bool ignoreMissingDevice = false, [System.Runtime.CompilerServices.CallerMemberName] string name = "")
        {
            this.name = name;
            string deviceName = Executor.Current.GetSetKey(this, name + ".DeviceName", string.Empty);

            this.messageMapper = new Dictionary<Tuple<int, ChannelCommand, int>, Action<ChannelMessage>>();

            this.midiMessages = new Subject<ChannelMessage>();

            int selectedDeviceId = -1;
            for (int i = 0; i < InputDevice.DeviceCount; i++)
            {
                var midiCap = InputDevice.GetDeviceCapabilities(i);

                if (midiCap.name == deviceName)
                {
                    selectedDeviceId = i;
                    break;
                }
            }

            if (selectedDeviceId == -1)
            {
                if (!ignoreMissingDevice)
                    throw new ArgumentException("Midi device not detected");
            }
            else
            {
                this.inputDevice = new InputDevice(selectedDeviceId);
                this.inputDevice.ChannelMessageReceived += inputDevice_ChannelMessageReceived;

                Executor.Current.SetKey(this, name + ".DeviceName", deviceName);
            }

            Executor.Current.Register(this);
        }

        public IObservable<ChannelMessage> MidiMessages
        {
            get
            {
                return this.midiMessages;
            }
        }

        private void inputDevice_ChannelMessageReceived(object sender, ChannelMessageEventArgs e)
        {
            log.Trace("Recv {4} cmd {0}, chn: {1}  data1: {2}  data2: {3}",
                e.Message.Command,
                e.Message.MidiChannel,
                e.Message.Data1,
                e.Message.Data2,
                Name);

            this.midiMessages.NotifyOn(TaskPoolScheduler.Default).OnNext(e.Message);

            try
            {
                var key = Tuple.Create(e.Message.MidiChannel, e.Message.Command, e.Message.Data1);

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
            if (this.inputDevice != null)
                this.inputDevice.StartRecording();
        }

        public void Stop()
        {
            if (this.inputDevice != null)
            {
                try
                {
                    this.inputDevice.StopRecording();
                    this.inputDevice.Close();
                }
                catch
                {
                }
            }
        }

        public IObservable<DoubleZeroToOne> Controller(int midiChannel, int controller)
        {
            var result = new Subject<DoubleZeroToOne>();

            this.messageMapper.Add(Tuple.Create(midiChannel, ChannelCommand.Controller, controller), m =>
            {
                result.NotifyOn(TaskPoolScheduler.Default).OnNext(new DoubleZeroToOne(m.Data2 / 127.0));
            });

            return result;
        }

        public IObservable<bool> Note(int midiChannel, int note)
        {
            var result = new Subject<bool>();

            this.messageMapper.Add(Tuple.Create(midiChannel, ChannelCommand.NoteOn, note), m =>
            {
                result.NotifyOn(TaskPoolScheduler.Default).OnNext(true);
            });

            this.messageMapper.Add(Tuple.Create(midiChannel, ChannelCommand.NoteOff, note), m =>
            {
                result.NotifyOn(TaskPoolScheduler.Default).OnNext(false);
            });

            return result;
        }

        private void WireUpDevice_Note(Animatroller.Framework.PhysicalDevice.DigitalInput device, int midiChannel, int note)
        {
            this.messageMapper.Add(Tuple.Create(midiChannel, ChannelCommand.NoteOn, note), m =>
                {
                    device.Trigger(true);
                });

            this.messageMapper.Add(Tuple.Create(midiChannel, ChannelCommand.NoteOff, note), m =>
                {
                    device.Trigger(false);
                });
        }

        public Animatroller.Framework.PhysicalDevice.DigitalInput AddDigitalInput_Note(DigitalInput logicalDevice, int midiChannel, int note)
        {
            var device = new Animatroller.Framework.PhysicalDevice.DigitalInput();

            WireUpDevice_Note(device, midiChannel, note);

            device.Connect(logicalDevice);

            return device;
        }

        public Animatroller.Framework.PhysicalDevice.AnalogInput AddAnalogInput_Note(AnalogInput logicalDevice, int midiChannel, int note)
        {
            var device = new Animatroller.Framework.PhysicalDevice.AnalogInput();

            this.messageMapper.Add(Tuple.Create(midiChannel, ChannelCommand.NoteOn, note), m =>
            {
                device.Trigger(m.Data2 / 127.0);
            });

            this.messageMapper.Add(Tuple.Create(midiChannel, ChannelCommand.NoteOff, note), m =>
            {
                device.Trigger(0);
            });

            device.Connect(logicalDevice);

            return device;
        }

        public Animatroller.Framework.PhysicalDevice.AnalogInput AddAnalogInput_Controller(AnalogInput logicalDevice, int midiChannel, int controller)
        {
            var device = new Animatroller.Framework.PhysicalDevice.AnalogInput();

            this.messageMapper.Add(Tuple.Create(midiChannel, ChannelCommand.Controller, controller), m =>
            {
                device.Trigger(m.Data2 / 127.0);
            });

            device.Connect(logicalDevice);

            return device;
        }

        public string Name
        { get { return this.name; } }
    }
}
