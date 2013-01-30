using System;

namespace Animatroller.Framework.Effect
{
    internal static class SweeperTables
    {
        public const int DataPoints = 1000;
        public static readonly double[] DataValues1 = new double[DataPoints];
        public static readonly double[] DataValues2 = new double[DataPoints];
        public static readonly double[] DataValues3 = new double[DataPoints];

        static SweeperTables()
        {
            for (int i = 0; i < DataPoints; i++)
            {
                DataValues1[i] = i / (double)(DataPoints - 1);

                if (i < (DataPoints / 2))
                    DataValues2[i] = 1 - 4 * i / (double)DataPoints;
                else
                    DataValues2[i] = -1 + 4 * (i - DataPoints / 2) / (double)DataPoints;

                DataValues3[i] = Math.Abs(1 - 2 * i / (double)DataPoints);
            }
        }

        public static int GetScaledIndex(int index, int max)
        {
            return Math.Min((int)(DataPoints * index / (double)max), DataPoints - 1);
        }
    }
}
