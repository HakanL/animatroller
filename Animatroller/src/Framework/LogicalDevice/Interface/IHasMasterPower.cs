using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reactive;
using System.Threading;
using System.Threading.Tasks;
using System.Drawing;
using Animatroller.Framework.Extensions;

namespace Animatroller.Framework.LogicalDevice
{
    public interface IHasMasterPower
    {
        IObserver<bool> InputMasterPower { get; }

        bool MasterPower { get; }
    }
}
