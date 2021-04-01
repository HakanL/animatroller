using System;
using System.Collections.Generic;
using System.Text;

namespace Animatroller.Processor
{
    public interface ITransformData : IBaseTransform
    {
        IList<Common.BaseDmxData> TransformData(Common.BaseDmxData dmxData);
    }
}
