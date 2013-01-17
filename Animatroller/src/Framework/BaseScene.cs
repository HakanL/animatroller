using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NLog;

namespace Animatroller.Framework
{
    public abstract class BaseScene : IScene
    {
        protected static Logger log = LogManager.GetCurrentClassLogger();

        protected TimeSpan S(double seconds)
        {
            return TimeSpan.FromSeconds(seconds);
        }

        protected TimeSpan MS(double seconds)
        {
            return TimeSpan.FromMilliseconds(seconds);
        }

        public abstract void Start();
        public abstract void Run();
        public abstract void Stop();
    }
}
