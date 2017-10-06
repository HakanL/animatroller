#define MANUAL_REDRAW
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
        private Color _colorGel;
        private bool _on = true;
        private string text;
        private Bitmap offScreenBitmap;
        private double _intensity;
        private double? _pan;
        private double? _tilt;
        private Font _tinyFont;
        private static SolidBrush blackSolidBrush = new SolidBrush(Color.Black);
        private static SolidBrush whiteSolidBrush = new SolidBrush(Color.White);

        /// <summary>
        /// Gets or Sets the color of the LED light
        /// </summary>
        [DefaultValue(typeof(Color), "153, 255, 54")]
        public Color Color
        {
            get { return _color; }
            set
            {
                if (_color != value)
                {
                    _color = value;
#if !MANUAL_REDRAW
                    this.Invalidate();  // Redraw the control
#endif
                }
            }
        }

        public Color ColorGel
        {
            get { return _colorGel; }
            set
            {
                if (_colorGel != value)
                {
                    _colorGel = value;
#if !MANUAL_REDRAW
                    this.Invalidate();  // Redraw the control
#endif
                }
            }
        }

        public double Intensity
        {
            get { return _intensity; }
            set
            {
                if (_intensity != value)
                {
                    _intensity = value;
#if !MANUAL_REDRAW
                    this.Invalidate();  // Redraw the control
#endif
                }
            }
        }

        public double? Pan
        {
            get { return _pan; }
            set
            {
                if (_pan != value)
                {
                    _pan = value;
#if !MANUAL_REDRAW
                    this.Invalidate();  // Redraw the control
#endif
                }
            }
        }

        public double? Tilt
        {
            get { return _tilt; }
            set
            {
                if (_tilt != value)
                {
                    _tilt = value;
#if !MANUAL_REDRAW
                    this.Invalidate();  // Redraw the control
#endif
                }
            }
        }

        public override string Text
        {
            get { return this.text; }
            set
            {
                if (this.text != value)
                {
                    this.text = value;
#if !MANUAL_REDRAW
                    this.Invalidate();  // Redraw the control
#endif
                }
            }
        }

        /// <summary>
        /// Gets or Sets whether the light is turned on
        /// </summary>
        public bool On
        {
            get { return _on; }
            set
            {
                if (_on != value)
                {
                    _on = value;
#if !MANUAL_REDRAW
                    this.Invalidate();  // Redraw the control
#endif
                }
            }
        }

#endregion

#region Constructor

        public SuperSimpleBulb()
        {
            //            SetStyle(ControlStyles.SupportsTransparentBackColor, true);
            this.Color = Color.FromArgb(255, 153, 255, 54);

            this._tinyFont = new Font(Font.FontFamily, 6);
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
                g.SmoothingMode = SmoothingMode.HighSpeed;
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
            const int intensityColumnWidth = 3;

            var drawColor = this.On ? this.Color : Color.Black;

            var paddedRectangle = new Rectangle(
                this.Padding.Left,
                this.Padding.Top,
                this.Width - (this.Padding.Left + this.Padding.Right) - 1,
                this.Height - (this.Padding.Top + this.Padding.Bottom) - 1);
            int width = (paddedRectangle.Width < paddedRectangle.Height) ? paddedRectangle.Width : paddedRectangle.Height;
            int offsetX = (paddedRectangle.Width - width) / 2;
            int offsetY = (paddedRectangle.Height - width) / 2;
            var drawRectangle = new Rectangle(
                paddedRectangle.X + offsetX + intensityColumnWidth,
                paddedRectangle.Y + offsetY,
                width - intensityColumnWidth,
                width);

            using (var drawColorBrush = new SolidBrush(drawColor))
                g.FillRectangle(drawColorBrush, drawRectangle);

            Color invertedColor;
            double brightness = Math.Max(Math.Max(drawColor.R, drawColor.G), drawColor.B) / 255.0;
            if (brightness < 0.8)
                invertedColor = Color.White;
            else
                invertedColor = Color.Black;

            using (var invertedBrush = new SolidBrush(invertedColor))
            {
                if (!string.IsNullOrEmpty(Text))
                {
                    var textSize = g.MeasureString(Text, Font);
                    var pos = new PointF((Width - textSize.Width) / 2, (Height - textSize.Height) / 2);

                    g.DrawString(Text, Font, invertedBrush, pos);
                }

                if (_pan.HasValue)
                {
                    string text = string.Format("P: {0:F0}", _pan.Value);

                    var textSize = g.MeasureString(text, _tinyFont);
                    var pos = new PointF(drawRectangle.Right - textSize.Width, 2);

                    g.DrawString(text, _tinyFont, invertedBrush, pos);
                }

                if (_tilt.HasValue)
                {
                    string text = string.Format("T: {0:F0}", _tilt.Value);

                    var textSize = g.MeasureString(text, _tinyFont);
                    var pos = new PointF(drawRectangle.Right - textSize.Width, 10);

                    g.DrawString(text, _tinyFont, invertedBrush, pos);
                }
            }

            // Draw Color Gel
            Rectangle gelRectangle = new Rectangle(drawRectangle.Right - 16, drawRectangle.Bottom - 16, 16, 16);
            using (var gelBrush = new SolidBrush(ColorGel))
                g.FillRectangle(gelBrush, gelRectangle);

            // Draw intensity
            Rectangle intensityRectangle1 = new Rectangle(paddedRectangle.X + offsetX, drawRectangle.Top, intensityColumnWidth, (int)(drawRectangle.Height * (1.0 - Intensity)));
            g.FillRectangle(blackSolidBrush, intensityRectangle1);

            Rectangle intensityRectangle2 = new Rectangle(intensityRectangle1.Left, intensityRectangle1.Bottom, intensityColumnWidth, drawRectangle.Height - intensityRectangle1.Height);
            g.FillRectangle(whiteSolidBrush, intensityRectangle2);
        }

#endregion
    }
}
