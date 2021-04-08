using System;
using System.Linq;
using System.IO;

namespace Animatroller.Common.IO
{
    public interface IFileReader
    {
        bool DataAvailable { get; }

        DmxDataOutputPacket ReadFrame();
    }
}
