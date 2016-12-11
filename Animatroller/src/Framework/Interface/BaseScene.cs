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
        protected static Random random = new Random();
        protected bool initialized = false;

        //protected HashFile.HashFile hashFile;

        public BaseScene()
        {
            //this.hashFile = new HashFile.HashFile();
            //this.hashFile.Initialize(this.GetType().Name, 50, 100);
        }

        public bool Initialized
        {
            get { return this.initialized; }
            set { this.initialized = value; }
        }

        protected static TimeSpan S(double seconds)
        {
            return TimeSpan.FromSeconds(seconds);
        }

        protected static TimeSpan MS(double seconds)
        {
            return TimeSpan.FromMilliseconds(seconds);
        }

        public virtual void Run()
        {
        }

        public virtual void Stop()
        {
            // Hack :)
            System.Threading.Thread.Sleep(200);
        }

        protected Executor Exec
        {
            get { return Executor.Current; }
        }
    }
}
