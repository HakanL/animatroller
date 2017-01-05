using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Animatroller.DMXplayer
{
    public class DmxFrame
    {
        public byte Start { get; set; }

        public uint TimestampMS { get; set; }

        public ushort Universe { get; set; }

        public ushort Len { get; set; }

        public byte[] Data { get; set; }

        public byte End { get; set; }
    }
}
