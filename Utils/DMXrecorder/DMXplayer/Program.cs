using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Animatroller.DMXplayer
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var acnStream = new AcnStream();

            var abc = new DmxPlayback(acnStream);

            abc.Load(@"C:\Temp\rainbow-loop.bin");

            acnStream.Start();

            abc.Run(false);

            Console.ReadLine();

            abc.Dispose();

            acnStream.Stop();
        }
    }
}
