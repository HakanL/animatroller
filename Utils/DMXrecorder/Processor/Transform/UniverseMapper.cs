using System;
using System.Collections.Generic;
using System.Text;
using Animatroller.Common;

namespace Animatroller.Processor.Transform
{
    public class UniverseMapper : ITransformData
    {
        private Dictionary<int, HashSet<int>> universeMapping;

        public IDictionary<int, HashSet<int>> UniverseMapping => this.universeMapping;

        public void AddUniverseMapping(int inputUniverse, int outputUniverse)
        {
            if (this.universeMapping == null)
                this.universeMapping = new Dictionary<int, HashSet<int>>();

            if (!this.universeMapping.TryGetValue(inputUniverse, out var outputList))
            {
                outputList = new HashSet<int>();
                this.universeMapping.Add(inputUniverse, outputList);
            }

            outputList.Add(outputUniverse);
        }

        public IList<DmxDataFrame> TransformData(DmxDataFrame dmxData)
        {
            var output = new List<DmxDataFrame>();

            if (this.universeMapping != null)
            {
                if (this.universeMapping.TryGetValue(dmxData.UniverseId, out var outputUniverses))
                {
                    foreach (int outputUniverse in outputUniverses)
                    {
                        output.Add(DmxDataFrame.CreateFrame(outputUniverse, dmxData.SyncAddress, dmxData.Data, dmxData.Destination));
                    }
                }
            }
            else
            {
                // No mapping
                output.Add(dmxData);
            }

            return output;
        }
    }
}
