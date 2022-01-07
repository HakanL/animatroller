using System;
using System.Collections.Generic;
using System.Text;

namespace Animatroller.Processor
{
    public interface ITransformTimestamp : IBaseTransform
    {
        double TransformTimestamp(Common.BaseDmxFrame dmxData, double timestampMS, ProcessorContext context);

        double TransformTimestamp2(Common.TransformFrame frame, ProcessorContext context);
    }
}
