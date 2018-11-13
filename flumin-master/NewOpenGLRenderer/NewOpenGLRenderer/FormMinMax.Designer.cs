namespace NewOpenGLRenderer {
    partial class FormMinMax {
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

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent() {
            this.components = new System.ComponentModel.Container();
            this.timer1 = new System.Windows.Forms.Timer(this.components);
            this.plot1 = new NewOpenGLRenderer.Plot();
            this.SuspendLayout();
            // 
            // timer1
            // 
            this.timer1.Enabled = true;
            this.timer1.Interval = 30;
            this.timer1.Tick += new System.EventHandler(this.timer1_Tick);
            // 
            // plot1
            // 
            this.plot1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.plot1.Antialias = true;
            this.plot1.BackColor = System.Drawing.Color.White;
            this.plot1.Border = new System.Windows.Forms.Padding(40, 10, 20, 50);
            this.plot1.GraphBackColor = System.Drawing.Color.White;
            this.plot1.Location = new System.Drawing.Point(12, 12);
            this.plot1.Name = "plot1";
            this.plot1.Set = null;
            this.plot1.Size = new System.Drawing.Size(595, 418);
            this.plot1.TabIndex = 0;
            this.plot1.GLLoaded += new System.EventHandler(this.plot1_GLLoaded);
            // 
            // FormMinMax
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(619, 442);
            this.Controls.Add(this.plot1);
            this.Name = "FormMinMax";
            this.Text = "FormMinMax";
            this.Load += new System.EventHandler(this.FormMinMax_Load);
            this.ResumeLayout(false);

        }

        #endregion

        private Plot plot1;
        private System.Windows.Forms.Timer timer1;
    }
}