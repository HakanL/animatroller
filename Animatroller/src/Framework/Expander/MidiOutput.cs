using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Animatroller.Framework.LogicalDevice;
using Animatroller.Framework.Extensions;
using NLog;
using Sanford.Multimedia.Midi;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace Animatroller.Framework.Expander
{
    public class MidiOutput : IPort, IRunnable
    {
        protected static Logger log = LogManager.GetCurrentClassLogger();
        private OutputDevice outputDevice;

        public MidiOutput(bool ignoreMissingDevice = false, [System.Runtime.CompilerServices.CallerMemberName] string name = "")
        {
            string deviceName = Executor.Current.GetSetKey(this, name + ".DeviceName", string.Empty);

            int selectedDeviceId = -1;
            for (int i = 0; i < OutputDevice.DeviceCount; i++)
            {
                var midiCap = OutputDevice.GetDeviceCapabilities(i);

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
                this.outputDevice = new OutputDevice(selectedDeviceId);

                this.outputDevice.Error += outputDevice_Error;

                Executor.Current.SetKey(this, name + ".DeviceName", deviceName);
            }

            Executor.Current.Register(this);
        }

        private void outputDevice_Error(object sender, Sanford.Multimedia.ErrorEventArgs e)
        {
            log.Trace("Got error from midi output {0}", e.Error);
        }

        public void Start()
        {
        }

        public void Stop()
        {
            if (this.outputDevice != null)
            {
                this.outputDevice.Close();
            }
        }

        public void Send(int midiChannel, int controller, byte value)
        {
            if (this.outputDevice == null)
                return;

            var builder = new ChannelMessageBuilder();
            builder.Command = ChannelCommand.Controller;
            builder.Data1 = controller;
            builder.Data2 = value;
            builder.MidiChannel = midiChannel;
            builder.Build();

            this.outputDevice.Send(builder.Result);
        }
    }
}
