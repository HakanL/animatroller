using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Animatroller.Framework.LogicalDevice.Event
{
    public class VideoCommandEventArgs : EventArgs
    {
        public enum Commands
        {
            Unknown = 0,
            PlayVideo
        };

        public Commands Command { get; private set; }
        public string VideoFile { get; private set; }

        public VideoCommandEventArgs(Commands command, string videoFile)
        {
            this.Command = command;
            this.VideoFile = videoFile;
        }
    }

    //public class VideoCommandValueEventArgs : VideoCommandEventArgs
    //{
    //    public double Value { get; private set; }

    //    public VideoCommandValueEventArgs(Commands command, double value)
    //        : base(command)
    //    {
    //        this.Value = value;
    //    }
    //}
}
