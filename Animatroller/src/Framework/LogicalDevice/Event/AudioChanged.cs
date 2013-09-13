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
            CueFX
        };

        public Commands Command { get; private set; }
        public string AudioFile { get; private set; }

        public AudioChangedEventArgs(Commands command, string audioFile)
        {
            this.Command = command;
            this.AudioFile = audioFile;
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
            NextBackground
        };

        public Commands Command { get; private set; }

        public AudioCommandEventArgs(Commands command)
        {
            this.Command = command;
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
