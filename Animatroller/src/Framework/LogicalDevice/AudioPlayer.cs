using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Animatroller.Framework.LogicalDevice
{
    public class AudioPlayer : ILogicalDevice
    {
        protected string name;

        public AudioPlayer(string name)
        {
            this.name = name;
            Executor.Current.Register(this);

            //TODO
        }

        public void StartDevice()
        {
        }

        public string Name
        {
            get { return this.name; }
        }
    }
}
