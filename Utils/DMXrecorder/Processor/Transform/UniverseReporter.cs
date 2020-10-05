using System;
using System.Collections.Generic;
using System.Text;

namespace Animatroller.Processor.Transform
{
    public class UniverseReporter : ITransform
    {
        private HashSet<int> universeIds = new HashSet<int>();

        public IList<(int UniverseId, byte[] DmxData)> Transform(int universeId, byte[] dmxData)
        {
            if (this.universeIds.Add(universeId))
            {
                Console.WriteLine($"Universe Id {universeId} found in input stream");
            }

            return null;
        }
    }
}
