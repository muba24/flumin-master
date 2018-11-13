namespace NewOpenGLRenderer {
    partial class FormOnly2D {
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
            this.plot1 = new NewOpenGLRenderer.Plot();
            this.timer1 = new System.Windows.Forms.Timer(this.components);
            this.SuspendLayout();
            // 
            // plot1
            // 
            this.plot1.Antialias = true;
            this.plot1.Border = new System.Windows.Forms.Padding(40, 10, 20, 50);
            this.plot1.GraphBackColor = System.Drawing.Color.White;
            this.plot1.BackColor = System.Drawing.Color.White;
            this.plot1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.plot1.Location = new System.Drawing.Point(0, 0);
            this.plot1.Name = "plot1";
            this.plot1.Set = null;
            this.plot1.Size = new System.Drawing.Size(284, 262);
            this.plot1.TabIndex = 0;
            this.plot1.GLLoaded += new System.EventHandler(this.plot1_GLLoaded);
            // 
            // timer1
            // 
            this.timer1.Enabled = true;
            this.timer1.Tick += new System.EventHandler(this.timer1_Tick);
            // 
            // FormOnly2D
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(284, 262);
            this.Controls.Add(this.plot1);
            this.Name = "FormOnly2D";
            this.Text = "FormOnly2D";
            this.ResumeLayout(false);

        }

        #endregion

        private Plot plot1;
        private System.Windows.Forms.Timer timer1;
    }
}