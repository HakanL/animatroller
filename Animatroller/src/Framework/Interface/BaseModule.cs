using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Serilog;

namespace Animatroller.Framework.Interface
{
    public abstract class BaseModule
    {
        protected ILogger log;
        protected static Random random = new Random();
        private string name;

        public BaseModule(string name)
        {
            this.name = name;
            this.log = Log.Logger;
        }

        public string Name
        {
            get { return this.name; }
        }

        protected static TimeSpan S(double seconds)
        {
            return TimeSpan.FromSeconds(seconds);
        }

        protected static TimeSpan MS(double seconds)
        {
            return TimeSpan.FromMilliseconds(seconds);
        }
    }
}
