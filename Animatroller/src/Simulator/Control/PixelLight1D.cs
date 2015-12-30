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
using System.Drawing.Drawing2D;

namespace Animatroller.Simulator.Control
{
    public partial class PixelLight1D : UserControl
    {
        private Bitmap outputBitmap;
        private Bitmap overlay;

        public PixelLight1D()
        {
            InitializeComponent();

            SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
        }

        public void SetImage(Bitmap image)
        {
            this.outputBitmap = image;

            Invalidate();
        }

        private void RopeLight2_Paint(object sender, PaintEventArgs e)
        {
            if (this.outputBitmap != null)
            {
                e.Graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
                e.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

                e.Graphics.DrawImage(this.outputBitmap, 0, 0, Width, Height);

                if (this.overlay != null)
                    e.Graphics.DrawImageUnscaled(this.overlay, 0, 0);
            }
        }

        private void RopeLight2_Resize(object sender, EventArgs e)
        {
            this.overlay = new Bitmap(Width, Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            for(int x = 3; x < Width; x += 4)
            {
                for (int y = 0; y < Height; y++)
                    this.overlay.SetPixel(x, y, SystemColors.Control);
            }
        }
    }
}
