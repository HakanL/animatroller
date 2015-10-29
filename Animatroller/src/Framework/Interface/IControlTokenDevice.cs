using System;
using System.Collections.Generic;
using System.Drawing;

namespace Animatroller.Framework
{
    public interface IControlTokenDevice : IControlToken
    {
        IData Data { get; }
    }
}
