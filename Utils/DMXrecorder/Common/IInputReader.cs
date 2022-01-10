using System;
using System.Linq;
using System.IO;

namespace Animatroller.Common
{
    public interface IInputReader
    {
        void Rewind();

        int FramesRead { get; }

        int TotalFrames { get; }

        InputFrame ReadFrame();

        bool HasSyncFrames { get; }
    }
}
