namespace Flumin {
    partial class RecordInfoView {
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
            this.objectListView = new BrightIdeasSoftware.ObjectListView();
            this.olvColumnName = ((BrightIdeasSoftware.OLVColumn)(new BrightIdeasSoftware.OLVColumn()));
            this.olvColumnSize = ((BrightIdeasSoftware.OLVColumn)(new BrightIdeasSoftware.OLVColumn()));
            this.olvColumnDuration = ((BrightIdeasSoftware.OLVColumn)(new BrightIdeasSoftware.OLVColumn()));
            this.textBoxPath = new System.Windows.Forms.TextBox();
            this.tableLayoutPanel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.objectListView)).BeginInit();
            this.SuspendLayout();
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.ColumnCount = 1;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.Controls.Add(this.objectListView, 0, 1);
            this.tableLayoutPanel1.Controls.Add(this.textBoxPath, 0, 0);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 2;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(493, 339);
            this.tableLayoutPanel1.TabIndex = 1;
            // 
            // objectListView
            // 
            this.objectListView.AllColumns.Add(this.olvColumnName);
            this.objectListView.AllColumns.Add(this.olvColumnSize);
            this.objectListView.AllColumns.Add(this.olvColumnDuration);
            this.objectListView.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.olvColumnName,
            this.olvColumnSize,
            this.olvColumnDuration});
            this.objectListView.Dock = System.Windows.Forms.DockStyle.Fill;
            this.objectListView.EmptyListMsg = "Leerer Ordner";
            this.objectListView.HideSelection = false;
            this.objectListView.Location = new System.Drawing.Point(3, 29);
            this.objectListView.Name = "objectListView";
            this.objectListView.ShowGroups = false;
            this.objectListView.Size = new System.Drawing.Size(487, 307);
            this.objectListView.TabIndex = 1;
            this.objectListView.UseCompatibleStateImageBehavior = false;
            this.objectListView.UseFilterIndicator = true;
            this.objectListView.UseFiltering = true;
            this.objectListView.View = System.Windows.Forms.View.Details;
            this.objectListView.ItemActivate += new System.EventHandler(this.objectListView_ItemActivate);
            this.objectListView.SelectedIndexChanged += new System.EventHandler(this.objectListView_SelectedIndexChanged);
            this.objectListView.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.objectListView_MouseDoubleClick);
            // 
            // olvColumnName
            // 
            this.olvColumnName.AspectName = "Name";
            this.olvColumnName.Text = "Name";
            this.olvColumnName.UseInitialLetterForGroup = true;
            this.olvColumnName.Width = 150;
            // 
            // olvColumnSize
            // 
            this.olvColumnSize.AspectName = "Extension";
            this.olvColumnSize.HeaderTextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.olvColumnSize.Text = "Size";
            this.olvColumnSize.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.olvColumnSize.Width = 80;
            // 
            // olvColumnDuration
            // 
            this.olvColumnDuration.AspectName = "Duration";
            this.olvColumnDuration.HeaderTextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.olvColumnDuration.Text = "Duration";
            this.olvColumnDuration.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.olvColumnDuration.Width = 100;
            // 
            // textBoxPath
            // 
            this.textBoxPath.Dock = System.Windows.Forms.DockStyle.Fill;
            this.textBoxPath.Location = new System.Drawing.Point(3, 3);
            this.textBoxPath.Name = "textBoxPath";
            this.textBoxPath.Size = new System.Drawing.Size(487, 20);
            this.textBoxPath.TabIndex = 0;
            this.textBoxPath.KeyDown += new System.Windows.Forms.KeyEventHandler(this.textBoxPath_KeyDown);
            this.textBoxPath.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.textBoxPath_KeyPress);
            // 
            // RecordInfoView
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(493, 339);
            this.Controls.Add(this.tableLayoutPanel1);
            this.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Name = "RecordInfoView";
            this.Text = "RecordInfoView";
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.objectListView)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private BrightIdeasSoftware.ObjectListView objectListView;
        private System.Windows.Forms.TextBox textBoxPath;
        private BrightIdeasSoftware.OLVColumn olvColumnName;
        private BrightIdeasSoftware.OLVColumn olvColumnSize;
        private BrightIdeasSoftware.OLVColumn olvColumnDuration;
    }
}