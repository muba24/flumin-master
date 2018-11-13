namespace Flumin {
    partial class BufferViewForm {
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
            this.flowLayoutPanelBars = new System.Windows.Forms.FlowLayoutPanel();
            this.timerRefresh = new System.Windows.Forms.Timer(this.components);
            this.SuspendLayout();
            // 
            // flowLayoutPanelBars
            // 
            this.flowLayoutPanelBars.AutoScroll = true;
            this.flowLayoutPanelBars.Dock = System.Windows.Forms.DockStyle.Fill;
            this.flowLayoutPanelBars.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.flowLayoutPanelBars.Location = new System.Drawing.Point(0, 0);
            this.flowLayoutPanelBars.Name = "flowLayoutPanelBars";
            this.flowLayoutPanelBars.Size = new System.Drawing.Size(284, 262);
            this.flowLayoutPanelBars.TabIndex = 0;
            // 
            // timerRefresh
            // 
            this.timerRefresh.Tick += new System.EventHandler(this.timerRefresh_Tick);
            // 
            // BufferViewForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(284, 262);
            this.Controls.Add(this.flowLayoutPanelBars);
            this.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Name = "BufferViewForm";
            this.Text = "BufferView";
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanelBars;
        private System.Windows.Forms.Timer timerRefresh;
    }
}