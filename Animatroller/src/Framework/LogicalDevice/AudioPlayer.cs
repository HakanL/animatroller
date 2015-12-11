using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Animatroller.Framework.LogicalDevice.Event;

namespace Animatroller.Framework.LogicalDevice
{
    public class AudioPlayer : BaseDevice
    {
        private bool silent;

        public event EventHandler<AudioChangedEventArgs> AudioChanged;
        public event EventHandler<AudioCommandEventArgs> ExecuteCommand;
        public event EventHandler<EventArgs> AudioTrackDone;
        public event EventHandler<AudioStartEventArgs> AudioTrackStart;

        public AudioPlayer([System.Runtime.CompilerServices.CallerMemberName] string name = "")
            : base(name)
        {
        }

        internal void RaiseAudioTrackStart(string fileName)
        {
            var handler = AudioTrackStart;
            if (handler != null)
                handler(this, new AudioStartEventArgs(fileName));
        }

        internal void RaiseAudioTrackDone()
        {
            var handler = AudioTrackDone;
            if (handler != null)
                handler(this, EventArgs.Empty);
        }

        protected virtual void RaiseAudioChanged(AudioChangedEventArgs.Commands command, string audioFile, double? leftVolume = null, double? rightVolume = null)
        {
            if (this.silent)
                return;

            var handler = AudioChanged;
            if (handler != null)
                handler(this, new AudioChangedEventArgs(command, audioFile, leftVolume, rightVolume));
        }

        protected virtual void RaiseExecuteCommand(AudioCommandEventArgs.Commands command)
        {
            if (this.silent)
                return;

            var handler = ExecuteCommand;
            if (handler != null)
                handler(this, new AudioCommandEventArgs(command));
        }

        protected virtual void RaiseExecuteCommand(AudioCommandEventArgs.Commands command, double value)
        {
            if (this.silent)
                return;

            var handler = ExecuteCommand;
            if (handler != null)
                handler(this, new AudioCommandValueEventArgs(command, value));
        }

        public AudioPlayer PlayEffect(string audioFile, double leftVolume, double rightVolume)
        {
            RaiseAudioChanged(AudioChangedEventArgs.Commands.PlayFX, audioFile, leftVolume, rightVolume);

            return this;
        }

        public AudioPlayer PlayEffect(string audioFile)
        {
            RaiseAudioChanged(AudioChangedEventArgs.Commands.PlayFX, audioFile);

            return this;
        }

        public AudioPlayer PlayEffect(string audioFile, double volume)
        {
            RaiseAudioChanged(AudioChangedEventArgs.Commands.PlayFX, audioFile, volume);

            return this;
        }

        public AudioPlayer PlayNewEffect(string audioFile, double leftVolume, double rightVolume)
        {
            RaiseAudioChanged(AudioChangedEventArgs.Commands.PlayNewFX, audioFile, leftVolume, rightVolume);

            return this;
        }

        public AudioPlayer PlayNewEffect(string audioFile)
        {
            RaiseAudioChanged(AudioChangedEventArgs.Commands.PlayNewFX, audioFile);

            return this;
        }

        public AudioPlayer PlayNewEffect(string audioFile, double volume)
        {
            RaiseAudioChanged(AudioChangedEventArgs.Commands.PlayNewFX, audioFile, volume);

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

        public AudioPlayer SetBackgroundVolume(double volume)
        {
            RaiseExecuteCommand(AudioCommandEventArgs.Commands.BackgroundVolume, volume);

            return this;
        }

        public AudioPlayer NextBackgroundTrack()
        {
            RaiseExecuteCommand(AudioCommandEventArgs.Commands.NextBackground);

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

        public AudioPlayer SetSilent(bool silent)
        {
            this.silent = silent;

            return this;
        }

        public AudioPlayer CueTrack(string audioFile)
        {
            RaiseAudioChanged(AudioChangedEventArgs.Commands.CueTrack, audioFile);

            return this;
        }

        /// <summary>
        /// Cue and play track
        /// </summary>
        /// <param name="audioFile"></param>
        /// <returns></returns>
        public AudioPlayer PlayTrack(string audioFile)
        {
            RaiseAudioChanged(AudioChangedEventArgs.Commands.PlayTrack, audioFile);

            return this;
        }

        public AudioPlayer ResumeTrack()
        {
            RaiseExecuteCommand(AudioCommandEventArgs.Commands.ResumeTrack);

            return this;
        }

        public AudioPlayer PauseTrack()
        {
            RaiseExecuteCommand(AudioCommandEventArgs.Commands.PauseTrack);

            return this;
        }

        protected override void UpdateOutput()
        {
            // Nothing to do here
        }
    }
}
