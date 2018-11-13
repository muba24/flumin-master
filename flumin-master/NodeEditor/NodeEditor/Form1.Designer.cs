namespace NodeEditorLib {
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
            this.nodeEditorControl1 = new NodeEditorLib.EditorControl.NodeEditorControl();
            this.SuspendLayout();
            // 
            // nodeEditorControl1
            // 
            this.nodeEditorControl1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(58)))), ((int)(((byte)(72)))), ((int)(((byte)(88)))));
            this.nodeEditorControl1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.nodeEditorControl1.Font = new System.Drawing.Font("Consolas", 10.2F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.nodeEditorControl1.Location = new System.Drawing.Point(0, 0);
            this.nodeEditorControl1.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.nodeEditorControl1.Name = "nodeEditorControl1";
            this.nodeEditorControl1.Size = new System.Drawing.Size(717, 419);
            this.nodeEditorControl1.TabIndex = 0;
            this.nodeEditorControl1.KeyDown += new System.Windows.Forms.KeyEventHandler(this.nodeEditorControl1_KeyDown);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(717, 419);
            this.Controls.Add(this.nodeEditorControl1);
            this.Name = "Form1";
            this.Text = "Form1";
            this.ResumeLayout(false);

        }

        #endregion

        private EditorControl.NodeEditorControl nodeEditorControl1;
    }
}

