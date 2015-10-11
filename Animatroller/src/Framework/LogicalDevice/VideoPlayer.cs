using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Animatroller.Framework.LogicalDevice.Event;

namespace Animatroller.Framework.LogicalDevice
{
    public class VideoPlayer : BaseDevice
    {
        public event EventHandler<VideoCommandEventArgs> ExecuteCommand;
        public event EventHandler<EventArgs> VideoTrackDone;

        public VideoPlayer([System.Runtime.CompilerServices.CallerMemberName] string name = "")
            : base(name)
        {
        }

        internal void RaiseVideoTrackDone()
        {
            var handler = VideoTrackDone;
            if (handler != null)
                handler(this, EventArgs.Empty);
        }

        protected virtual void RaiseExecuteCommand(VideoCommandEventArgs.Commands command, string videoFile)
        {
            var handler = ExecuteCommand;
            if (handler != null)
                handler(this, new VideoCommandEventArgs(command, videoFile));
        }

        public VideoPlayer PlayVideo(string videoFile)
        {
            RaiseExecuteCommand(VideoCommandEventArgs.Commands.PlayVideo, videoFile);

            return this;
        }
        protected override void UpdateOutput()
        {
            // Nothing to do here
        }
    }
}
