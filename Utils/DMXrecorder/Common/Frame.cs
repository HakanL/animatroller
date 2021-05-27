using System;
using System.Collections.Generic;
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
    }
}
