using System;
using System.Linq;
using System.IO;

namespace Animatroller.Framework.Import
{
    public interface IFileReader
    {
        bool DataAvailable { get; }

        void Rewind();

        DmxData ReadFrame();
    }

    public interface IFileReader2 : IFileReader
    {
        int TriggerUniverseId { get; }
    }
}
