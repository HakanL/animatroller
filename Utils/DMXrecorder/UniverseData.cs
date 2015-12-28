using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Animatroller.DMXrecorder
{
    public class UniverseData
    {
        public int Universe { get; private set; }

        public long SequenceHigh { get; set; }

        public byte LastSequenceLow { get; set; }

        public UniverseData(int universe)
        {
            Universe = universe;
        }
    }
}
