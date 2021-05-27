using System;
using System.Collections.Generic;
using System.Text;

namespace Animatroller.Processor
{
    public interface ITransformTimestamp : IBaseTransform
    {
        double TransformTimestamp(Common.BaseDmxFrame dmxData, double timestampMS, TransformContext context);

        double TransformTimestamp2(Common.OutputFrame frame, TransformContext context);
    }
}
