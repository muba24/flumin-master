namespace DeviceLibrary {
    partial class SettingsAnalogOut {
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
            this.buttonCancel = new System.Windows.Forms.Button();
            this.buttonOK = new System.Windows.Forms.Button();
            this.label3 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.numericUpDownBufferLength = new System.Windows.Forms.NumericUpDown();
            this.numericUpDownPreBufferLength = new System.Windows.Forms.NumericUpDown();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownBufferLength)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownPreBufferLength)).BeginInit();
            this.SuspendLayout();
            // 
            // buttonCancel
            // 
            this.buttonCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.buttonCancel.Location = new System.Drawing.Point(75, 66);
            this.buttonCancel.Name = "buttonCancel";
            this.buttonCancel.Size = new System.Drawing.Size(75, 23);
            this.buttonCancel.TabIndex = 15;
            this.buttonCancel.Text = "Cancel";
            this.buttonCancel.UseVisualStyleBackColor = true;
            this.buttonCancel.Click += new System.EventHandler(this.buttonCancel_Click);
            // 
            // buttonOK
            // 
            this.buttonOK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonOK.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.buttonOK.Location = new System.Drawing.Point(156, 66);
            this.buttonOK.Name = "buttonOK";
            this.buttonOK.Size = new System.Drawing.Size(75, 23);
            this.buttonOK.TabIndex = 14;
            this.buttonOK.Text = "OK";
            this.buttonOK.UseVisualStyleBackColor = true;
            this.buttonOK.Click += new System.EventHandler(this.buttonOK_Click);
            // 
            // label3
            // 
            this.label3.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(211, 10);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(20, 13);
            this.label3.TabIndex = 13;
            this.label3.Text = "ms";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(7, 10);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(70, 13);
            this.label2.TabIndex = 11;
            this.label2.Text = "Buffer length:";
            // 
            // label1
            // 
            this.label1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(211, 36);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(20, 13);
            this.label1.TabIndex = 18;
            this.label1.Text = "ms";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(7, 36);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(85, 13);
            this.label4.TabIndex = 16;
            this.label4.Text = "Prebuffer length:";
            // 
            // numericUpDownBufferLength
            // 
            this.numericUpDownBufferLength.Increment = new decimal(new int[] {
            100,
            0,
            0,
            0});
            this.numericUpDownBufferLength.Location = new System.Drawing.Point(97, 8);
            this.numericUpDownBufferLength.Maximum = new decimal(new int[] {
            10000,
            0,
            0,
            0});
            this.numericUpDownBufferLength.Name = "numericUpDownBufferLength";
            this.numericUpDownBufferLength.Size = new System.Drawing.Size(108, 20);
            this.numericUpDownBufferLength.TabIndex = 19;
            this.numericUpDownBufferLength.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.numericUpDownBufferLength.Value = new decimal(new int[] {
            1000,
            0,
            0,
            0});
            // 
            // numericUpDownPreBufferLength
            // 
            this.numericUpDownPreBufferLength.Increment = new decimal(new int[] {
            100,
            0,
            0,
            0});
            this.numericUpDownPreBufferLength.Location = new System.Drawing.Point(97, 34);
            this.numericUpDownPreBufferLength.Maximum = new decimal(new int[] {
            10000,
            0,
            0,
            0});
            this.numericUpDownPreBufferLength.Name = "numericUpDownPreBufferLength";
            this.numericUpDownPreBufferLength.Size = new System.Drawing.Size(108, 20);
            this.numericUpDownPreBufferLength.TabIndex = 20;
            this.numericUpDownPreBufferLength.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.numericUpDownPreBufferLength.Value = new decimal(new int[] {
            500,
            0,
            0,
            0});
            // 
            // SettingsAnalogOut
            // 
            this.AcceptButton = this.buttonOK;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.buttonCancel;
            this.ClientSize = new System.Drawing.Size(243, 101);
            this.Controls.Add(this.numericUpDownPreBufferLength);
            this.Controls.Add(this.numericUpDownBufferLength);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.buttonCancel);
            this.Controls.Add(this.buttonOK);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.Name = "SettingsAnalogOut";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Settings for NIDAQ Analog Output";
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownBufferLength)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownPreBufferLength)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button buttonCancel;
        private System.Windows.Forms.Button buttonOK;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.NumericUpDown numericUpDownBufferLength;
        private System.Windows.Forms.NumericUpDown numericUpDownPreBufferLength;
    }
}