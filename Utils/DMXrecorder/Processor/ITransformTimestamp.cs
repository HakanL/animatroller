using System;
using System.Collections.Generic;
using System.Text;

namespace Animatroller.Processor
{
    public interface ITransformTimestamp : IBaseTransform
    {
        double TransformTimestamp(Common.BaseDmxFrame dmxData, double timestampMS, TransformContext context);
    }
}
