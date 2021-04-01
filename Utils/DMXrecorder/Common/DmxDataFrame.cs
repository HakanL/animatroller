using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Threading.Tasks;

namespace Animatroller.Common
{
    public class DmxDataFrame : BaseDmxData
    {
        public double TimestampMS { get; set; }

        public DmxDataFrame()
        {
        }

        public DmxDataFrame(BaseDmxData dmxData, double timestampMS)
            : base(dmxData)
        {
            TimestampMS = timestampMS;
        }
    }
}
