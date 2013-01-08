namespace Animatroller.Simulator.Control
{
    partial class StrobeBulb
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
            this.components = new System.ComponentModel.Container();
            this.timerStrobe = new System.Windows.Forms.Timer(this.components);
            this.SuspendLayout();
            // 
            // timerStrobe
            // 
            this.timerStrobe.Tick += new System.EventHandler(this.timerStrobe_Tick);
            // 
            // SimpleLabelLight
            // 
            this.Name = "SimpleLabelLight";
            this.Size = new System.Drawing.Size(208, 155);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Timer timerStrobe;
    }
}
