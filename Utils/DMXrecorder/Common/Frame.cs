using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Animatroller.Common
{
    public abstract class Frame
    {
        public int SyncAddress { get; set; }

        public List<DmxDataFrame> DmxData { get; set; }

        public Frame()
        {
            DmxData = new List<DmxDataFrame>();
        }

        public bool IsAllBlack()
        {
            return DmxData.All(x => x.IsAllBlack());
        }
    }
}
