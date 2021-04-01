using System;
using System.Collections.Generic;
using System.Text;

namespace Animatroller.Processor
{
    public interface ITransformer
    {
        void Transform(TransformContext context, Common.DmxDataFrame dmxData, Action<Common.DmxDataPacket> action);

        void Simulate(TransformContext context, Common.DmxDataFrame dmxData, Action<Common.DmxDataPacket> action);
    }
}
