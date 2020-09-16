using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Animatroller.DMXplayer
{
    public interface IOutput : IDisposable
    {
        void SendDmx(int universe, byte[] data, byte? priority = null);

        IList<int> UsedUniverses { get; }
    }
}
