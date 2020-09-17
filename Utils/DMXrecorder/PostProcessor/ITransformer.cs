using System;
using System.Collections.Generic;
using System.Text;

namespace Animatroller.PostProcessor
{
    public interface ITransformer
    {
        void Transform(int universeId, byte[] dmxData, Action<int, byte[]> action);
    }
}
