using System;
using System.Collections.Generic;
using System.Text;
using Animatroller.Common;

namespace Animatroller.Processor.Transform
{
    public class BrightnessFixer : ITransformData
    {
        private readonly byte threshold;

        public BrightnessFixer(byte threshold = 10)
        {
            this.threshold = threshold;
        }

        public IList<DmxDataFrame> TransformData(DmxDataFrame dmxData)
        {
            for (int i = 0; i < dmxData.Data.Length; i++)
            {
                if (dmxData.Data[i] > this.threshold)
                    dmxData.Data[i] = 255;
                else
                    dmxData.Data[i] = 0;
            }

            return new List<DmxDataFrame>() { dmxData };
        }
    }
}
