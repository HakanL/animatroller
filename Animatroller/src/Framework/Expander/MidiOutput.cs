using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Animatroller.Framework.LogicalDevice;
using Animatroller.Framework.Extensions;
using Serilog;
using Sanford.Multimedia.Midi;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Diagnostics;

namespace Animatroller.Framework.Expander
{
    public class MidiOutput : IPort, IRunnable
    {
        protected ILogger log;
        private OutputDevice outputDevice;

        public MidiOutput(string deviceName = null, bool ignoreMissingDevice = false, [System.Runtime.CompilerServices.CallerMemberName] string name = "")
        {
            this.log = Log.Logger;
            string midiDeviceName = deviceName;
            if (string.IsNullOrEmpty(deviceName))
                midiDeviceName = Executor.Current.GetSetKey(this, name + ".DeviceName", string.Empty);

            int selectedDeviceId = -1;
            for (int i = 0; i < OutputDevice.DeviceCount; i++)
            {
                var midiCap = OutputDevice.GetDeviceCapabilities(i);

                if (midiCap.name == midiDeviceName)
                {
                    selectedDeviceId = i;
                    break;
                }
            }

            if (selectedDeviceId == -1)
            {
                if (!ignoreMissingDevice)
                    throw new ArgumentException("Midi device not detected");
                else
                    this.log.Warning("Midi device not detected");
            }
            else
            {
                this.outputDevice = new OutputDevice(selectedDeviceId);

                this.outputDevice.Error += outputDevice_Error;

                if (string.IsNullOrEmpty(deviceName))
                    Executor.Current.SetKey(this, name + ".DeviceName", midiDeviceName);
            }

            Executor.Current.Register(this);
        }

        private void outputDevice_Error(object sender, Sanford.Multimedia.ErrorEventArgs e)
        {
            this.log.Verbose(e.Error, "Got error from midi output {0}");
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
