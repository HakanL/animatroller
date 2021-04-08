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
    }
}
