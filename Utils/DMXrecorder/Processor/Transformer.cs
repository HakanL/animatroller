using System;
using System.Collections.Generic;
using System.Text;
using Animatroller.Common;

namespace Animatroller.Processor
{
    public class Transformer : ITransformer
    {
        private readonly IList<ITransform> transforms;
        private readonly Dictionary<int, long> sequencePerUniverse = new Dictionary<int, long>();

        public Transformer(IList<ITransform> transforms)
        {
            this.transforms = transforms ?? new List<ITransform>();
        }

        public void Transform(int universeId, byte[] dmxData, Action<int, byte[], long> action)
        {
            var data = new List<(int UniverseId, byte[] DmxData)>() { (universeId, dmxData) };

            foreach (var transform in this.transforms)
            {
                var outputData = new List<(int UniverseId, byte[] DmxData)>();

                foreach (var input in data)
                {
                    var newData = transform.Transform(input.UniverseId, input.DmxData);
                    if (newData == null)
                        outputData.Add(input);
                    else
                        outputData.AddRange(newData);
                }

                data = outputData;
            }

            foreach (var actionData in data)
            {
                this.sequencePerUniverse.TryGetValue(actionData.UniverseId, out long sequence);

                action(actionData.UniverseId, actionData.DmxData, sequence);

                sequence++;

                this.sequencePerUniverse[actionData.UniverseId] = sequence;
            }
        }
    }
}
