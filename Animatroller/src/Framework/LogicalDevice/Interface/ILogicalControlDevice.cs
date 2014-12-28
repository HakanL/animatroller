using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Drawing;
using Animatroller.Framework.Extensions;

namespace Animatroller.Framework.LogicalDevice
{
    public interface ILogicalControlDevice<T>
    {
        IObserver<T> Control { get; }
    }
}
