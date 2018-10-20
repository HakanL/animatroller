using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Animatroller.Framework.LogicalDevice.Event
{
    public class AudioChangedEventArgs : EventArgs
    {
        public enum Commands
        {
            Unknown = 0,
            PlayFX,
            CueFX,
            PlayNewFX,
            PlayTrack,
            CueTrack
        };

        public Commands Command { get; private set; }
        public string AudioFile { get; private set; }
        public double? LeftVolume { get; private set; }
        public double? RightVolume { get; private set; }

        public AudioChangedEventArgs(Commands command, string audioFile, double? leftVolume = null, double? rightVolume = null)
        {
            this.Command = command;
            this.AudioFile = audioFile;
            this.LeftVolume = leftVolume;
            this.RightVolume = rightVolume;
        }
    }

    public class AudioCommandEventArgs : EventArgs
    {
        public enum Commands
        {
            Unknown = 0,
            PlayBackground,
            PauseBackground,
            ResumeFX,
            PauseFX,
            BackgroundVolume,
            NextBackground,
            ResumeTrack,
            PauseTrack,
            EffectVolume,
            TrackVolume,
            StopFX,
            StopTrack
        };

        public Commands Command { get; private set; }

        public AudioCommandEventArgs(Commands command)
        {
            this.Command = command;
        }
    }

    public class AudioStartEventArgs : EventArgs
    {
        public string FileName { get; private set; }

        public AudioStartEventArgs(string fileName)
        {
            this.FileName = fileName;
        }
    }

    public class AudioCommandValueEventArgs : AudioCommandEventArgs
    {
        public double Value { get; private set; }

        public AudioCommandValueEventArgs(Commands command, double value)
            : base(command)
        {
            this.Value = value;
        }
    }
}
