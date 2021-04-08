using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Threading.Tasks;

namespace Animatroller.Common
{
    public class SyncFrame : BaseDmxFrame
    {
        public static SyncFrame CreateFrame(int syncAddress)
        {
            return new SyncFrame
            {
                SyncAddress = syncAddress
            };
        }
    }
}
