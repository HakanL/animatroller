using System;
using System.Collections.Generic;
using System.Text;

namespace Animatroller.Processor
{
    public interface ITransform
    {
        IList<(int UniverseId, byte[] DmxData)> Transform(int universeId, byte[] dmxData);
    }
}
