using NewOpenGLRenderer;
using System.Drawing;
using System.Windows.Forms;

namespace MetricTimeDisplay {
    partial class Display2Window {
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
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.flowLayoutPanel = new FlowLayoutPanel();
            this.components = new System.ComponentModel.Container();
            this.timerRefresh = new System.Windows.Forms.Timer(this.components);
            this.SuspendLayout();
            // 
            // timerRefresh
            // 
            this.timerRefresh.Interval = 30;
            this.timerRefresh.Tick += new System.EventHandler(this.timerRefresh_Tick);

            //
            // flowLayoutPanel
            //
            this.flowLayoutPanel.Visible = true;
            this.flowLayoutPanel.FlowDirection = FlowDirection.TopDown;
            this.flowLayoutPanel.Dock = DockStyle.Fill;
            this.flowLayoutPanel.AutoScroll = true;

            //
            // _plotCtrl
            //
            _plotCtrl = new Plot();
            _plotCtrl.BackColor = Color.White;
            _plotCtrl.Border = new Padding(40, 10, 20, 50);
            _plotCtrl.GraphBackColor = Color.LightGray;
            _plotCtrl.Dock = DockStyle.Fill;
            _plotCtrl.Antialias = true;
            _plotCtrl.Visible = true;


            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.ColumnCount = 1;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 2;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 70F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 30F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(284, 262);
            this.tableLayoutPanel1.TabIndex = 0;
            this.tableLayoutPanel1.Controls.Add(_plotCtrl);
            this.tableLayoutPanel1.Controls.Add(this.flowLayoutPanel);
            this.tableLayoutPanel1.SetRow(this._plotCtrl, 0);
            this.tableLayoutPanel1.SetRow(this.flowLayoutPanel, 1);

            // 
            // Display2Window
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(284, 262);
            this.Controls.Add(this.tableLayoutPanel1);
            this.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Name = "Display2Window";
            this.Text = "Display2Window";
            this.Load += new System.EventHandler(this.Display2Window_Load);
            this.Shown += new System.EventHandler(this.Display2Window_Shown);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Timer timerRefresh;
        private TableLayoutPanel tableLayoutPanel1;
        private FlowLayoutPanel flowLayoutPanel;
        private Plot _plotCtrl;

    }
}