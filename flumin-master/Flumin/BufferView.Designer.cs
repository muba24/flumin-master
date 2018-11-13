namespace Flumin {
    partial class BufferView {
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
            this.labelNodeName = new System.Windows.Forms.Label();
            this.labelPortName = new System.Windows.Forms.Label();
            this.progressBarFill = new System.Windows.Forms.ProgressBar();
            this.labelTime = new System.Windows.Forms.Label();
            this.pictureBoxCanWrite = new System.Windows.Forms.PictureBox();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxCanWrite)).BeginInit();
            this.SuspendLayout();
            // 
            // labelNodeName
            // 
            this.labelNodeName.AutoSize = true;
            this.labelNodeName.Location = new System.Drawing.Point(3, 0);
            this.labelNodeName.Name = "labelNodeName";
            this.labelNodeName.Size = new System.Drawing.Size(64, 13);
            this.labelNodeName.TabIndex = 0;
            this.labelNodeName.Text = "Node Name";
            // 
            // labelPortName
            // 
            this.labelPortName.AutoSize = true;
            this.labelPortName.Location = new System.Drawing.Point(12, 13);
            this.labelPortName.Name = "labelPortName";
            this.labelPortName.Size = new System.Drawing.Size(57, 13);
            this.labelPortName.TabIndex = 1;
            this.labelPortName.Text = "Port Name";
            // 
            // progressBarFill
            // 
            this.progressBarFill.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.progressBarFill.Location = new System.Drawing.Point(103, 0);
            this.progressBarFill.Maximum = 10000;
            this.progressBarFill.Name = "progressBarFill";
            this.progressBarFill.Size = new System.Drawing.Size(123, 23);
            this.progressBarFill.Style = System.Windows.Forms.ProgressBarStyle.Continuous;
            this.progressBarFill.TabIndex = 2;
            // 
            // labelTime
            // 
            this.labelTime.AutoSize = true;
            this.labelTime.Location = new System.Drawing.Point(12, 26);
            this.labelTime.Name = "labelTime";
            this.labelTime.Size = new System.Drawing.Size(33, 13);
            this.labelTime.TabIndex = 3;
            this.labelTime.Text = "Time:";
            // 
            // pictureBoxCanWrite
            // 
            this.pictureBoxCanWrite.BackColor = System.Drawing.Color.Red;
            this.pictureBoxCanWrite.Location = new System.Drawing.Point(159, 26);
            this.pictureBoxCanWrite.Name = "pictureBoxCanWrite";
            this.pictureBoxCanWrite.Size = new System.Drawing.Size(67, 13);
            this.pictureBoxCanWrite.TabIndex = 4;
            this.pictureBoxCanWrite.TabStop = false;
            // 
            // BufferView
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.pictureBoxCanWrite);
            this.Controls.Add(this.labelTime);
            this.Controls.Add(this.progressBarFill);
            this.Controls.Add(this.labelPortName);
            this.Controls.Add(this.labelNodeName);
            this.Name = "BufferView";
            this.Size = new System.Drawing.Size(229, 50);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxCanWrite)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label labelNodeName;
        private System.Windows.Forms.Label labelPortName;
        private System.Windows.Forms.ProgressBar progressBarFill;
        private System.Windows.Forms.Label labelTime;
        private System.Windows.Forms.PictureBox pictureBoxCanWrite;
    }
}
