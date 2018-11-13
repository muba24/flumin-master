namespace NewOpenGLRenderer {
    partial class PlotAxis {
        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing) {
            if (disposing && (components != null)) {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent() {
            this.components = new System.ComponentModel.Container();
            this.timerAxisRefresh = new System.Windows.Forms.Timer(this.components);
            this.SuspendLayout();
            // 
            // timerAxisRefresh
            // 
            this.timerAxisRefresh.Interval = 30;
            this.timerAxisRefresh.Tick += new System.EventHandler(this.timerAxisRefresh_Tick);
            // 
            // PlotAxis
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.DoubleBuffered = true;
            this.Name = "PlotAxis";
            this.Paint += new System.Windows.Forms.PaintEventHandler(this.PlotAxis_Paint);
            this.MouseDown += new System.Windows.Forms.MouseEventHandler(this.PlotAxis_MouseDown);
            this.MouseLeave += new System.EventHandler(this.PlotAxis_MouseLeave);
            this.MouseMove += new System.Windows.Forms.MouseEventHandler(this.PlotAxis_MouseMove);
            this.MouseUp += new System.Windows.Forms.MouseEventHandler(this.PlotAxis_MouseUp);
            this.Resize += new System.EventHandler(this.PlotAxis_Resize);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Timer timerAxisRefresh;
    }
}
