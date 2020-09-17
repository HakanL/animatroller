using System;
using System.Collections.Generic;
using System.Text;

namespace Animatroller.PostProcessor
{
    public interface ITransform
    {
        IList<(int UniverseId, byte[] DmxData)> Transform(int universeId, byte[] dmxData);
    }
}
