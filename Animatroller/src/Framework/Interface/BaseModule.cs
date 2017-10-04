using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Animatroller.Framework.Interface
{
    public abstract class BaseModule
    {
        protected static TimeSpan S(double seconds)
        {
            return TimeSpan.FromSeconds(seconds);
        }

        protected static TimeSpan MS(double seconds)
        {
            return TimeSpan.FromMilliseconds(seconds);
        }
    }
}
