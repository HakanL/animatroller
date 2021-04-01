using System;
using System.Linq;
using System.IO;

namespace Animatroller.Common
{
    public interface IFileWriter
    {
        void Header(int universeId);

        void Output(DmxDataPacket dmxData);

        void Footer(int universeId);
    }
}
