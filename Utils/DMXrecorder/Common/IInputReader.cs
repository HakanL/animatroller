using System;
using System.Linq;
using System.IO;

namespace Animatroller.Common
{
    public interface IInputReader
    {
        void Rewind();

        DmxDataOutputPacket ReadFrame();

        int FramesRead { get; }

        InputFrame ReadFrame2();

        InputFrame PeekFrame2();

        bool HasSyncFrames { get; }
    }
}
