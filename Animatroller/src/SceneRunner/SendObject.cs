using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Animatroller.SceneRunner
{
    public class SendObject
    {
        public SendControl SendControl { get; set; }

        public byte[] LastHash { get; set; }

        public DateTime LastSend { get; set; }
    }
}
