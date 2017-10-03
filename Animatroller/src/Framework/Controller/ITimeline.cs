using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Serilog;

namespace Animatroller.Framework.Controller
{
    public interface ITimeline
    {
        void Stop();

        Task Start(long offsetMs = 0, TimeSpan? duration = null);
    }
}
