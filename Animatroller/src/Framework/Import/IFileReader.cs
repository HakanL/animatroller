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

    public interface IFileReader3 : IFileReader2
    {
        byte[] ReadFullFrame(out long timestampMS);

        int FrameSize { get; }

        (int UniverseId, int FSeqChannel)[] GetFrameLayout();
    }
}
