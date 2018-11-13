namespace NewOpenGLRenderer {
    partial class Form1 {
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
            this.button1 = new System.Windows.Forms.Button();
            this.tmrPhase = new System.Windows.Forms.Timer(this.components);
            this.plot2 = new NewOpenGLRenderer.Plot();
            this.SuspendLayout();
            // 
            // button1
            // 
            this.button1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.button1.Location = new System.Drawing.Point(810, 472);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(91, 23);
            this.button1.TabIndex = 2;
            this.button1.Text = "Pause";
            this.button1.UseVisualStyleBackColor = true;
            // 
            // tmrPhase
            // 
            this.tmrPhase.Enabled = true;
            this.tmrPhase.Interval = 35;
            this.tmrPhase.Tick += new System.EventHandler(this.tmrPhase_Tick);
            // 
            // plot2
            // 
            this.plot2.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.plot2.Antialias = true;
            this.plot2.BackColor = System.Drawing.Color.White;
            this.plot2.Border = new System.Windows.Forms.Padding(40, 10, 20, 50);
            this.plot2.GraphBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(224)))), ((int)(((byte)(224)))), ((int)(((byte)(224)))));
            this.plot2.Location = new System.Drawing.Point(0, 0);
            this.plot2.Name = "plot2";
            this.plot2.Set = null;
            this.plot2.Size = new System.Drawing.Size(913, 466);
            this.plot2.TabIndex = 1;
            this.plot2.GLLoaded += new System.EventHandler(this.plot2_GLLoaded);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(913, 507);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.plot2);
            this.Name = "Form1";
            this.Text = "Form1";
            this.ResumeLayout(false);

        }

        #endregion

        private Plot plot2;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Timer tmrPhase;
    }
}

