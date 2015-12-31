using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Animatroller.PostProcessor
{
    public class FindLoop
    {
        private Common.BinaryFileReader fileReader;

        public FindLoop(Common.BinaryFileReader fileReader)
        {
            this.fileReader = fileReader;
        }

        public void Execute()
        {
            // Find all universes and the first data in each
            var firstData = new Dictionary<int, byte[]>();
            var currentData = new Dictionary<int, byte[]>();

            var diffList = new List<Tuple<double, long>>();

            while (this.fileReader.DataAvailable)
            {
                long pos = this.fileReader.Position;

                var data = this.fileReader.ReadFrame();

                if (data.DataType != Common.DmxData.DataTypes.FullFrame)
                    continue;

                if (!firstData.ContainsKey(data.Universe))
                {
                    firstData.Add(data.Universe, data.Data);
                }
                else
                {
                    currentData[data.Universe] = data.Data;

                    // Compare
                    int? diff = null;
                    int dataElements = 0;
                    foreach (var kvp in firstData)
                    {
                        byte[] compareData;
                        if (!currentData.TryGetValue(kvp.Key, out compareData))
                        {
                            diff = null;
                            break;
                        }

                        if (!diff.HasValue)
                            diff = 0;
                        int maxLen = Math.Min(kvp.Value.Length, compareData.Length);
                        for (int i = 0; i < maxLen; i++)
                            diff += Math.Abs(kvp.Value[i] - compareData[i]);
                        dataElements += maxLen;
                    }

                    if (diff.HasValue)
                        diffList.Add(Tuple.Create((double)diff.Value / dataElements, pos));
                }
            }

            diffList = diffList.OrderBy(x => x.Item1).ToList();

            foreach (var match in diffList.Take(10))
            {
                Console.WriteLine("Pos {0:P2}   Mismatch {1:P2}   TrimPos: {2}", (double)match.Item2 / this.fileReader.Length, match.Item1 / 100.0,
                    match.Item2);
            }
        }
    }
}
