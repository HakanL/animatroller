﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Threading.Tasks;

namespace Animatroller.Common
{
    public class DmxDataPacket
    {
        public double TimestampMS { get; set; }

        public BaseDmxFrame Content { get; set; }
    }
}
