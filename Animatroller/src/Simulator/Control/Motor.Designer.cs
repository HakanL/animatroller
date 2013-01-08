namespace Animatroller.Simulator.Control
{
    partial class Motor
    {
        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.labelMotorPos = new System.Windows.Forms.Label();
            this.trackBarMotor = new System.Windows.Forms.TrackBar();
            ((System.ComponentModel.ISupportInitialize)(this.trackBarMotor)).BeginInit();
            this.SuspendLayout();
            // 
            // labelMotorPos
            // 
            this.labelMotorPos.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.labelMotorPos.AutoSize = true;
            this.labelMotorPos.Location = new System.Drawing.Point(102, 0);
            this.labelMotorPos.Name = "labelMotorPos";
            this.labelMotorPos.Size = new System.Drawing.Size(33, 13);
            this.labelMotorPos.TabIndex = 8;
            this.labelMotorPos.Text = "{Pos}";
            this.labelMotorPos.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // trackBarMotor
            // 
            this.trackBarMotor.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.trackBarMotor.Enabled = false;
            this.trackBarMotor.Location = new System.Drawing.Point(0, 16);
            this.trackBarMotor.Minimum = -10;
            this.trackBarMotor.Name = "trackBarMotor";
            this.trackBarMotor.Size = new System.Drawing.Size(147, 45);
            this.trackBarMotor.TabIndex = 7;
            // 
            // Motor
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.labelMotorPos);
            this.Controls.Add(this.trackBarMotor);
            this.Name = "Motor";
            this.Size = new System.Drawing.Size(147, 61);
            ((System.ComponentModel.ISupportInitialize)(this.trackBarMotor)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label labelMotorPos;
        private System.Windows.Forms.TrackBar trackBarMotor;
    }
}
