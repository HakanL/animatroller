using System;
using System.Collections.Generic;
using System.Text;

namespace Animatroller.Processor.Transform
{
    public class UniverseReporter : ITransformData
    {
        private HashSet<int> universeIds = new HashSet<int>();

        public IList<Common.DmxDataFrame> TransformData(Common.DmxDataFrame dmxData)
        {
            if (dmxData.UniverseId.HasValue && this.universeIds.Add(dmxData.UniverseId.Value))
            {
                Console.WriteLine($"Universe Id {dmxData.UniverseId} found in input stream");
            }

            return null;
        }
    }
}
