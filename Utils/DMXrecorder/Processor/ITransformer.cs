using System;
using System.Collections.Generic;
using System.Text;

namespace Animatroller.Processor
{
    public interface ITransformer
    {
        void Transform(TransformContext context, Common.DmxDataPacket dmxData, Action<Common.BaseDmxFrame> action = null);

        void Simulate(TransformContext context, Common.DmxDataPacket dmxData, Action<Common.BaseDmxFrame> action);
    }
}
