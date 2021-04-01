using System;
using System.Collections.Generic;
using System.Text;
using Animatroller.Common;

namespace Animatroller.Processor.Transform
{
    public class BrightnessFixer : ITransformData
    {
        private byte threshold;

        public BrightnessFixer(byte threshold = 10)
        {
            this.threshold = threshold;
        }

        public IList<Common.BaseDmxData> TransformData(Common.BaseDmxData dmxData)
        {
            for (int i = 0; i < dmxData.Data.Length; i++)
            {
                if (dmxData.Data[i] > this.threshold)
                    dmxData.Data[i] = 255;
                else
                    dmxData.Data[i] = 0;
            }

            return new List<Common.BaseDmxData>() { dmxData };
        }
    }
}
