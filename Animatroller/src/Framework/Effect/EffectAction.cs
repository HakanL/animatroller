using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Animatroller.Framework.Effect
{
    public static class EffectAction
    {
        public delegate void Action(double zeroToOne, double negativeOneToOne, double oneToZeroToOne, bool forced, long totalTicks, bool final);
    }
}
