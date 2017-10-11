using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Serilog;

namespace Animatroller.Framework
{
    public abstract class BaseScene : BaseClass, IScene
    {
        protected bool initialized = false;

        public BaseScene()
        {
        }

        public bool Initialized
        {
            get { return this.initialized; }
            set { this.initialized = value; }
        }

        public virtual void Run()
        {
        }

        public virtual void Stop()
        {
            // Hack :)
            System.Threading.Thread.Sleep(200);
        }
    }
}
