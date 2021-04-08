using System;
using System.Linq;
using System.IO;

namespace Animatroller.Common.IO
{
    public interface IFileWriter
    {
        void Header(int universeId);

        void Output(DmxDataOutputPacket dmxData);

        void Footer(int universeId);
    }
}
