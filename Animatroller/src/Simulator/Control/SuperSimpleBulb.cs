using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using System.Diagnostics;

namespace Animatroller.Simulator.Control.Bulb
{
    /// <summary>
    /// The LEDBulb is a .Net control for Windows Forms that emulates an
    /// LED light with two states On and Off.  The purpose of the control is to 
    /// provide a sleek looking representation of an LED light that is sizable, 
    /// has a transparent background and can be set to different colors.  
    /// </summary>
    public partial class SuperSimpleBulb : System.Windows.Forms.Control
    {

        #region Public and Private Members

        private Color _color;
        private bool _on = true;
        private string text;
        private Bitmap offScreenBitmap;

        /// <summary>
        /// Gets or Sets the color of the LED light
        /// </summary>
        [DefaultValue(typeof(Color), "153, 255, 54")]
        public Color Color
        {
            get { return _color; }
            set
            {
                _color = value;
                this.Invalidate();	// Redraw the control
            }
        }

        public override string Text
        {
            get { return this.text; }
            set
            {
                this.text = value;
                this.Invalidate();
            }
        }

        /// <summary>
        /// Gets or Sets whether the light is turned on
        /// </summary>
        public bool On { get { return _on; } set { _on = value; this.Invalidate(); } }

        #endregion

        #region Constructor

        public SuperSimpleBulb()
        {
            //            SetStyle(ControlStyles.SupportsTransparentBackColor, true);
            this.Color = Color.FromArgb(255, 153, 255, 54);
        }

        #endregion

        #region Transpanency Methods

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= 0x20;
                return cp;
            }
        }
        protected override void OnMove(EventArgs e)
        {
            RecreateHandle();
        }
        protected override void OnPaintBackground(PaintEventArgs e)
        {

        }

        #endregion

        #region Drawing Methods

        /// <summary>
        /// Handles the Paint event for this UserControl
        /// </summary>
        protected override void OnPaint(PaintEventArgs e)
        {
            if (this.offScreenBitmap == null ||
                this.ClientRectangle.Width != this.offScreenBitmap.Width ||
                this.ClientRectangle.Height != this.offScreenBitmap.Height)
            {
                if (this.offScreenBitmap != null)
                    this.offScreenBitmap.Dispose();

                this.offScreenBitmap = new Bitmap(this.ClientRectangle.Width, this.ClientRectangle.Height);
            }

            // Create an offscreen graphics object for double buffering
            using (System.Drawing.Graphics g = Graphics.FromImage(this.offScreenBitmap))
            {
                g.SmoothingMode = SmoothingMode.HighQuality;
                // Draw the control
                drawControl(g);
                // Draw the image to the screen
                e.Graphics.DrawImageUnscaled(this.offScreenBitmap, 0, 0);
            }
        }

        /// <summary>
        /// Renders the control to an image
        /// </summary>
        /// <param name="g"></param>
        private void drawControl(Graphics g)
        {
            var drawColor = this.On ? this.Color : Color.Black;

            Rectangle paddedRectangle = new Rectangle(this.Padding.Left, this.Padding.Top, this.Width - (this.Padding.Left + this.Padding.Right) - 1, this.Height - (this.Padding.Top + this.Padding.Bottom) - 1);
            int width = (paddedRectangle.Width < paddedRectangle.Height) ? paddedRectangle.Width : paddedRectangle.Height;
            int offsetX = (paddedRectangle.Width - width) / 2;
            int offsetY = (paddedRectangle.Height - width) / 2;
            Rectangle drawRectangle = new Rectangle(paddedRectangle.X + offsetX, paddedRectangle.Y + offsetY, width, width);

            g.FillRectangle(new SolidBrush(drawColor), drawRectangle);

            if (!string.IsNullOrEmpty(Text))
            {
                double brightness = Math.Max(Math.Max(drawColor.R, drawColor.G), drawColor.B) / 255.0;

                Color fontColor;
                if (brightness < 0.5)
                    fontColor = Color.White;
                else
                    fontColor = Color.Black;

                var textSize = g.MeasureString(Text, Font);
                var pos = new PointF((Width - textSize.Width) / 2, (Height - textSize.Height) / 2);

                g.DrawString(Text, Font, new SolidBrush(fontColor), pos);
            }
        }

        #endregion
    }
}
