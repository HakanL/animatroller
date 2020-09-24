using System;
using System.Collections.Generic;
using System.Text;

namespace Animatroller.Processor
{
    public interface ITransformer
    {
        void Transform(int universeId, byte[] dmxData, Action<int, byte[]> action);
    }
}
