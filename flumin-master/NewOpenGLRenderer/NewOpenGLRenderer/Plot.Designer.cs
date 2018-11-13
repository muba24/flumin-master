namespace NewOpenGLRenderer {
    partial class Plot {
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
            this.tmrAxisRefresh = new System.Windows.Forms.Timer(this.components);
            this.toolStrip1 = new System.Windows.Forms.ToolStrip();
            this.plotAxisVert = new NewOpenGLRenderer.PlotAxis();
            this.plotAxisHorz = new NewOpenGLRenderer.PlotAxis();
            this.SuspendLayout();
            // 
            // tmrAxisRefresh
            // 
            this.tmrAxisRefresh.Interval = 30;
            this.tmrAxisRefresh.Tick += new System.EventHandler(this.tmrAxisRefresh_Tick);
            // 
            // toolStrip1
            // 
            this.toolStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.toolStrip1.Location = new System.Drawing.Point(0, 0);
            this.toolStrip1.Name = "toolStrip1";
            this.toolStrip1.Size = new System.Drawing.Size(839, 25);
            this.toolStrip1.TabIndex = 3;
            this.toolStrip1.Text = "toolStrip1";
            // 
            // plotAxisVert
            // 
            this.plotAxisVert.Axis = null;
            this.plotAxisVert.Location = new System.Drawing.Point(39, 68);
            this.plotAxisVert.Margin = new System.Windows.Forms.Padding(5, 5, 5, 5);
            this.plotAxisVert.Name = "plotAxisVert";
            this.plotAxisVert.Size = new System.Drawing.Size(72, 208);
            this.plotAxisVert.TabIndex = 5;
            this.plotAxisVert.AxisChanged += new System.EventHandler<NewOpenGLRenderer.PlotAxis.AxisChangedEventArgs>(this.plotAxisVert_AxisChanged);
            // 
            // plotAxisHorz
            // 
            this.plotAxisHorz.Axis = null;
            this.plotAxisHorz.Location = new System.Drawing.Point(156, 324);
            this.plotAxisHorz.Margin = new System.Windows.Forms.Padding(5, 5, 5, 5);
            this.plotAxisHorz.Name = "plotAxisHorz";
            this.plotAxisHorz.Size = new System.Drawing.Size(600, 47);
            this.plotAxisHorz.TabIndex = 4;
            this.plotAxisHorz.AxisChanged += new System.EventHandler<NewOpenGLRenderer.PlotAxis.AxisChangedEventArgs>(this.plotAxisHorz_AxisChanged);
            // 
            // Plot
            // 
            this.AllowDrop = true;
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.White;
            this.Controls.Add(this.plotAxisVert);
            this.Controls.Add(this.plotAxisHorz);
            this.Controls.Add(this.toolStrip1);
            this.DoubleBuffered = true;
            this.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.Name = "Plot";
            this.Size = new System.Drawing.Size(839, 405);
            this.Load += new System.EventHandler(this.Plot_Load);
            this.DragDrop += new System.Windows.Forms.DragEventHandler(this.Plot_DragDrop);
            this.DragEnter += new System.Windows.Forms.DragEventHandler(this.Plot_DragEnter);
            this.Paint += new System.Windows.Forms.PaintEventHandler(this.Plot_Paint);
            this.Resize += new System.EventHandler(this.Plot_Resize);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.Timer tmrAxisRefresh;
        private System.Windows.Forms.ToolStrip toolStrip1;
        private PlotAxis plotAxisHorz;
        private PlotAxis plotAxisVert;
    }
}
