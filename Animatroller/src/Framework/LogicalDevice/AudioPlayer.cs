using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Animatroller.Framework.LogicalDevice.Event;

namespace Animatroller.Framework.LogicalDevice
{
    public class AudioPlayer : ILogicalDevice
    {
        protected string name;

        public event EventHandler<AudioChangedEventArgs> AudioChanged;
        public event EventHandler<AudioCommandEventArgs> ExecuteCommand;

        public AudioPlayer(string name)
        {
            this.name = name;
            Executor.Current.Register(this);
        }

        public void StartDevice()
        {
        }

        public string Name
        {
            get { return this.name; }
        }

        protected virtual void RaiseAudioChanged(AudioChangedEventArgs.Commands command, string audioFile)
        {
            var handler = AudioChanged;
            if (handler != null)
                handler(this, new AudioChangedEventArgs(command, audioFile));
        }

        protected virtual void RaiseExecuteCommand(AudioCommandEventArgs.Commands command)
        {
            var handler = ExecuteCommand;
            if (handler != null)
                handler(this, new AudioCommandEventArgs(command));
        }

        public AudioPlayer PlayEffect(string audioFile)
        {
            RaiseAudioChanged(AudioChangedEventArgs.Commands.PlayFX, audioFile);

            return this;
        }

        public AudioPlayer PlayBackground()
        {
            RaiseExecuteCommand(AudioCommandEventArgs.Commands.PlayBackground);

            return this;
        }

        public AudioPlayer PauseBackground()
        {
            RaiseExecuteCommand(AudioCommandEventArgs.Commands.PauseBackground);

            return this;
        }

        public AudioPlayer SetBackgroundVolume(byte volume)
        {
//            SendCommand(string.Format("BV,{0}", volume));

            return this;
        }

        public AudioPlayer CueFX(string audioFile)
        {
            RaiseAudioChanged(AudioChangedEventArgs.Commands.CueFX, audioFile);

            return this;
        }

        public AudioPlayer ResumeFX()
        {
            RaiseExecuteCommand(AudioCommandEventArgs.Commands.ResumeFX);

            return this;
        }

        public AudioPlayer PauseFX()
        {
            RaiseExecuteCommand(AudioCommandEventArgs.Commands.PauseFX);

            return this;
        }
    }
}
