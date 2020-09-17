using System;
using System.Collections.Generic;
using System.Text;
using Animatroller.Common;

namespace Animatroller.PostProcessor
{
    public class UniverseMapper : ITransform
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

        public IList<(int UniverseId, byte[] DmxData)> Transform(int universeId, byte[] dmxData)
        {
            var output = new List<(int UniverseId, byte[] DmxData)>();

            if (this.universeMapping != null)
            {
                if (this.universeMapping.TryGetValue(universeId, out var outputUniverses))
                {
                    foreach (int outputUniverse in outputUniverses)
                    {
                        output.Add((outputUniverse, dmxData));
                    }
                }
            }
            else
            {
                // No mapping
                output.Add((universeId, dmxData));
            }

            return output;
        }
    }
}
