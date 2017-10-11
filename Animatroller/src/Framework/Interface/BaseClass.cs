using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Serilog;

namespace Animatroller.Framework
{
    public abstract class BaseClass
    {
        protected ILogger log;
        protected static Random random = new Random();

        public BaseClass()
        {
            this.log = Log.Logger;
        }

        protected static TimeSpan S(double seconds)
        {
            return TimeSpan.FromSeconds(seconds);
        }

        protected static TimeSpan MS(double seconds)
        {
            return TimeSpan.FromMilliseconds(seconds);
        }

        protected Executor Exec
        {
            get { return Executor.Current; }
        }
    }
}
