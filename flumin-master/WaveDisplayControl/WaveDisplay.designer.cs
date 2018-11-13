namespace WaveDisplayControl {
    partial class WaveDisplay {
        /// <summary> 
        /// Erforderliche Designervariable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Verwendete Ressourcen bereinigen.
        /// </summary>
        /// <param name="disposing">True, wenn verwaltete Ressourcen gelöscht werden sollen; andernfalls False.</param>
        protected override void Dispose(bool disposing) {
            if (disposing && (components != null)) {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Vom Komponenten-Designer generierter Code

        /// <summary> 
        /// Erforderliche Methode für die Designerunterstützung. 
        /// Der Inhalt der Methode darf nicht mit dem Code-Editor geändert werden.
        /// </summary>
        private void InitializeComponent() {
            this.SuspendLayout();
            // 
            // WaveDisplay
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.DoubleBuffered = true;
            this.Name = "WaveDisplay";
            this.Size = new System.Drawing.Size(424, 248);
            this.Load += new System.EventHandler(this.WaveDisplay_Load);
            this.Click += new System.EventHandler(this.WaveDisplay_Click);
            this.Paint += new System.Windows.Forms.PaintEventHandler(this.WaveDisplay_Paint);
            this.MouseDown += new System.Windows.Forms.MouseEventHandler(this.WaveDisplay_MouseDown);
            this.MouseMove += new System.Windows.Forms.MouseEventHandler(this.WaveDisplay_MouseMove);
            this.MouseUp += new System.Windows.Forms.MouseEventHandler(this.WaveDisplay_MouseUp);
            this.Resize += new System.EventHandler(this.WaveDisplay_Resize);
            this.ResumeLayout(false);

        }

        #endregion
    }
}
