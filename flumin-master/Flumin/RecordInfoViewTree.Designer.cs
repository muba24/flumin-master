namespace Flumin {
    partial class RecordInfoViewTree {
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
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.treeListView = new BrightIdeasSoftware.TreeListView();
            this.columnName = ((BrightIdeasSoftware.OLVColumn)(new BrightIdeasSoftware.OLVColumn()));
            this.columnDuration = ((BrightIdeasSoftware.OLVColumn)(new BrightIdeasSoftware.OLVColumn()));
            this.columnSize = ((BrightIdeasSoftware.OLVColumn)(new BrightIdeasSoftware.OLVColumn()));
            this.textBoxPath = new System.Windows.Forms.TextBox();
            this.tableLayoutPanel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.treeListView)).BeginInit();
            this.SuspendLayout();
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.ColumnCount = 1;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.Controls.Add(this.treeListView, 0, 1);
            this.tableLayoutPanel1.Controls.Add(this.textBoxPath, 0, 0);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 2;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(481, 360);
            this.tableLayoutPanel1.TabIndex = 1;
            // 
            // treeListView
            // 
            this.treeListView.AllColumns.Add(this.columnName);
            this.treeListView.AllColumns.Add(this.columnDuration);
            this.treeListView.AllColumns.Add(this.columnSize);
            this.treeListView.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnName,
            this.columnDuration,
            this.columnSize});
            this.treeListView.Dock = System.Windows.Forms.DockStyle.Fill;
            this.treeListView.FullRowSelect = true;
            this.treeListView.IsSimpleDragSource = true;
            this.treeListView.Location = new System.Drawing.Point(3, 32);
            this.treeListView.MultiSelect = false;
            this.treeListView.Name = "treeListView";
            this.treeListView.OwnerDraw = true;
            this.treeListView.ShowGroups = false;
            this.treeListView.Size = new System.Drawing.Size(475, 325);
            this.treeListView.TabIndex = 3;
            this.treeListView.UseCompatibleStateImageBehavior = false;
            this.treeListView.View = System.Windows.Forms.View.Details;
            this.treeListView.VirtualMode = true;
            this.treeListView.ItemActivate += new System.EventHandler(this.treeListView_ItemActivate);
            // 
            // columnName
            // 
            this.columnName.AspectName = "Name";
            this.columnName.Text = "Name";
            this.columnName.Width = 90;
            // 
            // columnDuration
            // 
            this.columnDuration.AspectName = "Duration";
            this.columnDuration.Text = "Duration";
            this.columnDuration.Width = 84;
            // 
            // columnSize
            // 
            this.columnSize.AspectName = "Size";
            this.columnSize.Text = "Size";
            // 
            // textBoxPath
            // 
            this.textBoxPath.Dock = System.Windows.Forms.DockStyle.Fill;
            this.textBoxPath.Location = new System.Drawing.Point(3, 3);
            this.textBoxPath.Name = "textBoxPath";
            this.textBoxPath.Size = new System.Drawing.Size(475, 23);
            this.textBoxPath.TabIndex = 1;
            // 
            // RecordInfoViewTree
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 17F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(481, 360);
            this.Controls.Add(this.tableLayoutPanel1);
            this.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Name = "RecordInfoViewTree";
            this.Text = "RecordInfoViewTree";
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.treeListView)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private BrightIdeasSoftware.OLVColumn columnName;
        private BrightIdeasSoftware.OLVColumn columnDuration;
        private BrightIdeasSoftware.OLVColumn columnSize;
        private System.Windows.Forms.TextBox textBoxPath;
        private BrightIdeasSoftware.TreeListView treeListView;
    }
}