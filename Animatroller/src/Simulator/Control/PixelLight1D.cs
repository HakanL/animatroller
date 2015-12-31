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
        private int scale;

        public PixelLight1D(int scale = 4)
        {
            InitializeComponent();

            SetStyle(ControlStyles.OptimizedDoubleBuffer, true);

            this.scale = scale;
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

        private void PixelLight1D_Resize(object sender, EventArgs e)
        {
            if (this.scale == 0)
                return;

            this.overlay = new Bitmap(Width, Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            using (var g = Graphics.FromImage(this.overlay))
            using (var p = new Pen(Color.Black))
            {
                for (int x = this.scale - 1; x < Width; x += this.scale)
                {
                    g.DrawLine(p, x, 0, x, Height - 1);
                }
            }
        }
    }
}
