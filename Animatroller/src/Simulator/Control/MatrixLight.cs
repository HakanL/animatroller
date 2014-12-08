using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Animatroller.Simulator.Control
{
    public partial class MatrixLight : UserControl
    {
        private Color[,] colors;

        public MatrixLight(int pixelWidth, int pixelHeight)
        {
            InitializeComponent();

            PixelWidth = pixelWidth;
            PixelHeight = pixelHeight;

            this.colors = new Color[PixelWidth, PixelHeight];
            for (int x = 0; x < PixelWidth; x++)
                for (int y = 0; y < PixelHeight; y++)
                    this.colors[x, y] = Color.Black;

            tableLayoutPanel.ColumnStyles.Clear();
            tableLayoutPanel.RowStyles.Clear();

            int width = pixelWidth * 8;
            int height = pixelHeight * 8;

            int lastPos = 0;
            float counter = 0;
            for (int x = 0; x < PixelWidth; x++)
            {
                counter += ((float)width / PixelWidth);

                tableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, (int)(counter - lastPos)));

                lastPos = (int)counter;
            }

            lastPos = 0;
            counter = 0;
            for (int y = 0; y < PixelHeight; y++)
            {
                counter += ((float)height / PixelHeight);

                tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, (int)(counter - lastPos)));

                lastPos = (int)counter;
            }

            tableLayoutPanel.ColumnCount = PixelWidth;
            tableLayoutPanel.RowCount = PixelHeight;
        }

        public int PixelWidth { get; private set; }

        public int PixelHeight { get; private set; }

        public int Pixels
        {
            get { return PixelWidth * PixelHeight; }
        }

        public void SetPixel(int x, int y, Color color)
        {
            this.colors[x, y] = color;

            tableLayoutPanel.Invalidate();
        }

        public void SetPixels(Color[,] colors)
        {
            Array.Copy(colors, this.colors, PixelWidth * PixelHeight);

            tableLayoutPanel.Invalidate();
        }

        private void tableLayoutPanel_CellPaint(object sender, TableLayoutCellPaintEventArgs e)
        {
            if (e.Column >= 0 && e.Column < PixelWidth && e.Row >= 0 && e.Row < PixelHeight)
            {
                var rect = new Rectangle(e.CellBounds.Location, new Size(e.CellBounds.Width - 1, e.CellBounds.Height - 1));

                e.Graphics.FillRectangle(new SolidBrush(this.colors[e.Column, e.Row]), rect);
            }
        }
    }
}
