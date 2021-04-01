using System;
using System.Collections.Generic;
using System.Text;

namespace Animatroller.Processor
{
    public interface ITransformTimestamp : IBaseTransform
    {
        double TransformTimestamp(Common.BaseDmxData dmxData, double timestampMS, TransformContext context);
    }
}
