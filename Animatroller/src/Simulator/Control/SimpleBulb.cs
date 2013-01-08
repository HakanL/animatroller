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
    public partial class SimpleBulb : System.Windows.Forms.Control
    {

		#region Public and Private Members

		private Color _color;
		private bool _on = true;
        private string text;

		/// <summary>
		/// Gets or Sets the color of the LED light
		/// </summary>
		[DefaultValue(typeof(Color), "153, 255, 54")]
		public Color Color { 
			get { return _color; } 
			set { 
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

        public SimpleBulb()
        {
			SetStyle(ControlStyles.SupportsTransparentBackColor, true);
			this.Color = Color.FromArgb(255, 153, 255, 54);
		}
		
		#endregion

		#region Transpanency Methods
		
		protected override CreateParams CreateParams {
			get {
				CreateParams cp = base.CreateParams;
				cp.ExStyle |= 0x20;
				return cp;
			}
		}
		protected override void OnMove(EventArgs e) {
			RecreateHandle();
		}
		protected override void OnPaintBackground(PaintEventArgs e) {

		}
		
		#endregion

		#region Drawing Methods

		/// <summary>
		/// Handles the Paint event for this UserControl
		/// </summary>
		protected override void OnPaint(PaintEventArgs e){
			// Create an offscreen graphics object for double buffering
			Bitmap offScreenBmp = new Bitmap(this.ClientRectangle.Width, this.ClientRectangle.Height);
			using (System.Drawing.Graphics g = Graphics.FromImage(offScreenBmp)) {
				g.SmoothingMode = SmoothingMode.HighQuality;
				// Draw the control
				drawControl(g);
				// Draw the image to the screen
				e.Graphics.DrawImageUnscaled(offScreenBmp, 0, 0);
			}
		}

		/// <summary>
		/// Renders the control to an image
		/// </summary>
		/// <param name="g"></param>
		private void drawControl(Graphics g) {
            var drawColor = this.On ? this.Color : Color.Black;

			Rectangle paddedRectangle = new Rectangle(this.Padding.Left, this.Padding.Top, this.Width - (this.Padding.Left + this.Padding.Right) - 1, this.Height - (this.Padding.Top + this.Padding.Bottom) - 1);
			int width = (paddedRectangle.Width < paddedRectangle.Height) ? paddedRectangle.Width : paddedRectangle.Height;
            int offsetX = (paddedRectangle.Width - width) / 2;
            int offsetY = (paddedRectangle.Height - width) / 2;
			Rectangle drawRectangle = new Rectangle(paddedRectangle.X + offsetX, paddedRectangle.Y + offsetY, width, width);

			// Draw the background ellipse
			if (drawRectangle.Width < 1) drawRectangle.Width = 1;
			if (drawRectangle.Height < 1) drawRectangle.Height = 1;
			g.FillEllipse(new SolidBrush(drawColor), drawRectangle);

			// Draw the glow gradient
			GraphicsPath path = new GraphicsPath();
			path.AddEllipse(drawRectangle);
			PathGradientBrush pathBrush = new PathGradientBrush(path);
            pathBrush.CenterColor = drawColor;
            pathBrush.SurroundColors = new Color[] { Color.FromArgb(0, drawColor) };
			g.FillEllipse(pathBrush, drawRectangle);

			// Set the clip boundary  to the edge of the ellipse
			GraphicsPath gp = new GraphicsPath();
			gp.AddEllipse(drawRectangle);
			g.SetClip(gp);

			// Draw the white reflection gradient
			GraphicsPath path1 = new GraphicsPath();
			Rectangle whiteRect = new Rectangle(drawRectangle.X - Convert.ToInt32(drawRectangle.Width * .15F), drawRectangle.Y - Convert.ToInt32(drawRectangle.Width * .15F), Convert.ToInt32(drawRectangle.Width*.8F), Convert.ToInt32(drawRectangle.Height*.8F));
			path1.AddEllipse(whiteRect);
			PathGradientBrush pathBrush1 = new PathGradientBrush(path);
			pathBrush1.CenterColor = Color.FromArgb(180, 255, 255, 255);
			pathBrush1.SurroundColors = new Color[] { Color.FromArgb(0, 255, 255, 255) };
			g.FillEllipse(pathBrush1, whiteRect);

			// Draw the border
			float w = drawRectangle.Width;
			g.SetClip(this.ClientRectangle);
			if (this.On) g.DrawEllipse(new Pen(Color.FromArgb(85, Color.Black),1F), drawRectangle);

            if (!string.IsNullOrEmpty(Text))
            {
                var textSize = g.MeasureString(Text, Font);
                var pos = new PointF((Width - textSize.Width) / 2, (Height - textSize.Height) / 2);
                g.DrawString(Text, Font, new SolidBrush(Color.Black), pos);
            }
		}

		#endregion
	}
}
