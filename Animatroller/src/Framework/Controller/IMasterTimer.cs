using System;

namespace Animatroller.Framework.Controller
{
    public interface IMasterTimer
    {
        long ElapsedMs { get; }

        IObservable<long> Output { get; }

        int IntervalMs { get; }
    }
}
