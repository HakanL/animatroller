using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Animatroller.Framework
{
    public class ControlTokenValue<T>
    {
        public IControlToken ControlToken { get; set; }

        public T Value { get; set; }
    }
}
