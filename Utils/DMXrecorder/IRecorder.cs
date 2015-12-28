using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Animatroller.DMXrecorder
{
    public interface IRecorder : IDisposable
    {
        void StartRecord();

        void StopRecord();
    }
}
