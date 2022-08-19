using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Threading.Tasks;

namespace Animatroller.Common
{
    public class BaseDmxFrame
    {
        public int SyncAddress { get; set; }

        public System.Net.IPAddress Destination { get; set; }

        public BaseDmxFrame()
        {
        }
    }
}
