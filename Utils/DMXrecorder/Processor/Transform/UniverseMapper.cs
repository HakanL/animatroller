using System;
using System.Collections.Generic;
using System.Text;
using Animatroller.Common;
using System.Net;

namespace Animatroller.Processor.Transform
{
    public class UniverseMapper : ITransformData
    {
        private Dictionary<(IPAddress Address, int? UniverseId), HashSet<(IPAddress Address, bool Multicast, int? UniverseId)>> universeMapping;

        public IDictionary<(IPAddress Address, int? UniverseId), HashSet<(IPAddress Address, bool Multicast, int? UniverseId)>> UniverseMapping => this.universeMapping;

        public void AddUniverseMapping(IPAddress inputAddress, int? inputUniverseId, IPAddress outputAddress, bool outputMulticast, int? outputUniverseId)
        {
            this.universeMapping ??= new Dictionary<(IPAddress Address, int? UniverseId), HashSet<(IPAddress Address, bool Multicast, int? UniverseId)>>();

            if (!this.universeMapping.TryGetValue((inputAddress, inputUniverseId), out var outputList))
            {
                outputList = new HashSet<(IPAddress Address, bool Multicast, int? UniverseId)>();
                this.universeMapping.Add((inputAddress, inputUniverseId), outputList);
            }

            outputList.Add((outputAddress, outputMulticast, outputUniverseId));
        }

        public IList<DmxDataFrame> TransformData(DmxDataFrame dmxData)
        {
            var output = new List<DmxDataFrame>();

            HashSet<(IPAddress Address, bool Multicast, int? UniverseId)> outputUniverses = null;

            if (this.universeMapping != null)
            {
                // Exact match
                if (!this.universeMapping.TryGetValue((dmxData.Destination, dmxData.UniverseId), out outputUniverses))
                {
                    if (!this.universeMapping.TryGetValue((null, dmxData.UniverseId), out outputUniverses))
                    {
                        this.universeMapping.TryGetValue((dmxData.Destination, null), out outputUniverses);
                    }
                }
            }
            else
            {
                // No mapping
                output.Add(dmxData);
            }

            if (outputUniverses != null)
            {
                foreach (var kvp in outputUniverses)
                {
                    IPAddress destination;
                    if (kvp.Address == null)
                    {
                        if (kvp.Multicast)
                            destination = null;
                        else
                            destination = dmxData.Destination;
                    }
                    else
                    {
                        destination = kvp.Address;
                    }

                    output.Add(DmxDataFrame.CreateFrame(kvp.UniverseId ?? dmxData.UniverseId, dmxData.SyncAddress, dmxData.Data, destination));
                }
            }

            return output;
        }
    }
}
