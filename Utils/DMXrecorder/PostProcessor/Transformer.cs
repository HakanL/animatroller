using System;
using System.Collections.Generic;
using System.Text;
using Animatroller.Common;

namespace Animatroller.PostProcessor
{
    public class Transformer : ITransformer
    {
        private readonly IList<ITransform> transforms;

        public Transformer(IList<ITransform> transforms)
        {
            this.transforms = transforms ?? new List<ITransform>();
        }

        public void Transform(int universeId, byte[] dmxData, Action<int, byte[]> action)
        {
            var data = new List<(int UniverseId, byte[] DmxData)>() { (universeId, dmxData) };

            foreach (var transform in this.transforms)
            {
                var outputData = new List<(int UniverseId, byte[] DmxData)>();

                foreach (var input in data)
                {
                    var newData = transform.Transform(input.UniverseId, input.DmxData);
                    outputData.AddRange(newData);
                }

                data = outputData;
            }

            foreach (var actionData in data)
            {
                action(actionData.UniverseId, actionData.DmxData);
            }
        }
    }
}
