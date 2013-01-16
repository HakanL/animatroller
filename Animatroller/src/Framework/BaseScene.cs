using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Animatroller.Framework
{
    public abstract class BaseScene : IScene
    {
        protected TimeSpan Seconds(double seconds)
        {
            return TimeSpan.FromSeconds(seconds);
        }

        public abstract void Start();
        public abstract void Run();
        public abstract void Stop();
    }
}
