using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Serilog;

namespace Animatroller.Framework.Interface
{
    public abstract class BaseModule : BaseClass
    {
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
    }
}
