using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using System.Text;
using System.Threading.Tasks;

namespace Animatroller.Framework.LogicalDevice
{
    public class PixelMapper2D
    {
        public enum PixelOrders
        {
            HorizontalLineTopLeft,
            HorizontalSnakeTopLeft
        }

        private int[] lookupX;
        private int[] lookupY;
        private int[,] lookupXY;
        private byte[] streamOutput;
        private int width;
        private int height;

        public PixelMapper2D(int width, int height, PixelOrders pixelOrder)
        {
            this.width = width;
            this.height = height;

            this.lookupX = new int[width * height];
            this.lookupY = new int[width * height];
            this.lookupXY = new int[width, height];
            this.streamOutput = new byte[width * height * 3];

            PopulateFromPixelOrder(pixelOrder);
        }

        private void PopulateFromPixelOrder(PixelOrders pixelOrder)
        {
            int pos = 0;

            var populate = new Action<int, int, int>((x, y, p) =>
                {
                    this.lookupX[p] = x;
                    this.lookupY[p] = y;

                    this.lookupXY[x, y] = p;
                });

            if (pixelOrder == PixelOrders.HorizontalLineTopLeft)
            {
                for (int y = 0; y < height; y++)
                    for (int x = 0; x < width; x++)
                    {
                        populate(x, y, pos);

                        pos++;
                    }
            }
            else
                if (pixelOrder == PixelOrders.HorizontalSnakeTopLeft)
                {
                    for (int y = 0; y < height; y++)
                    {
                        for (int x = 0; x < width; x++)
                        {
                            populate(x, y, pos);

                            pos++;
                        }

                        if (++y < height)
                        {
                            for (int x = width - 1; x >= 0; x--)
                            {
                                populate(x, y, pos);

                                pos++;
                            }
                        }
                    }
                }
                else
                    throw new NotImplementedException();
        }

        public void FromRGBByteArray(byte[] pixelData, int pixelOffset, Action<int, int, Color, double> setPixel)
        {
            int readPos = 0;
            int pixelCount = pixelData.Length / 3;

            for (int i = 0; i < pixelCount; i++)
            {
                int x = this.lookupX[pixelOffset + i];
                int y = this.lookupY[pixelOffset + i];

                var color = Color.FromArgb(pixelData[readPos++], pixelData[readPos++], pixelData[readPos++]);

                setPixel(x, y, color, 1.0);
            }
        }

        public byte[] GetByteArray(Color[,] colorData)
        {
            for (int y = 0; y < this.height; y++)
                for (int x = 0; x < this.width; x++)
                {
                    int pos = this.lookupXY[x, y] * 3;
                    var color = colorData[x, y];

                    this.streamOutput[pos] = color.R;
                    this.streamOutput[pos + 1] = color.G;
                    this.streamOutput[pos + 2] = color.B;
                }

            return this.streamOutput;
        }
    }
}
