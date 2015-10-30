using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Animatroller.Framework.LogicalDevice;

namespace Animatroller.Simulator.Control
{
    public partial class RopeLight : UserControl
    {
        private int pixels;
        private Color[] colors;

        public RopeLight()
        {
            InitializeComponent();
        }

        public int Pixels
        {
            get { return pixels; }
            set
            {
                if (value < 1)
                    throw new ArgumentOutOfRangeException();

                this.pixels = value;
                this.colors = new Color[this.pixels];
                for (int i = 0; i < this.colors.Length; i++)
                    this.colors[i] = Color.Black;

                tableLayoutPanel.ColumnStyles.Clear();
                int lastX = 0;
                float counter = 0;
                for (int i = 0; i < this.colors.Length; i++)
                {
                    counter += ((float)tableLayoutPanel.Width / this.pixels);
                    tableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, (int)(counter - lastX)));
                    lastX = (int)counter;
                }

                tableLayoutPanel.ColumnCount = this.pixels;
            }
        }

        public void SetPixel(int channel, Color color)
        {
            if (channel < 0 || channel >= this.colors.Length)
                throw new ArgumentOutOfRangeException();

            this.colors[channel] = color;

            tableLayoutPanel.Invalidate();
        }

        public void SetPixels(int startChannel, Color[] colors)
        {
            for (int i = 0; i < colors.Length; i++)
            {
                int channel = i + startChannel;
                if (channel < 0 || channel >= this.colors.Length)
                    continue;

                this.colors[channel] = colors[i];
            }

            tableLayoutPanel.Invalidate();
        }

        public void SetPixels(int startChannel, ColorBrightness[] pixelData)
        {
            for (int i = 0; i < pixelData.Length; i++)
            {
                int channel = i + startChannel;
                if (channel < 0 || channel >= this.colors.Length)
                    continue;

                this.colors[channel] = Framework.PhysicalDevice.BaseLight.GetColorFromColorBrightness(pixelData[i]);
            }

            tableLayoutPanel.Invalidate();
        }

        private void tableLayoutPanel_CellPaint(object sender, TableLayoutCellPaintEventArgs e)
        {
            if (e.Column >= 0 && e.Column < this.colors.Length)
            {
                var rect = new Rectangle(e.CellBounds.Location, new Size(e.CellBounds.Width, e.CellBounds.Height));

                e.Graphics.FillRectangle(new SolidBrush(this.colors[e.Column]), rect);
            }
        }
    }
}
