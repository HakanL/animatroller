using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Animatroller.Framework
{
    public sealed class SimulatorButtonTypeAttribute : Attribute
    {
        public SimulatorButtonTypes Type { get; private set; }

        public SimulatorButtonTypeAttribute(SimulatorButtonTypes type)
        {
            Type = type;
        }
    }
}
