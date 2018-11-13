namespace CustomPropertyGrid {
    public partial class PropertyGrid {
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
            this.objectListView = new BrightIdeasSoftware.ObjectListView();
            this.olvColumnTitle = ((BrightIdeasSoftware.OLVColumn)(new BrightIdeasSoftware.OLVColumn()));
            this.olvColumnValue = ((BrightIdeasSoftware.OLVColumn)(new BrightIdeasSoftware.OLVColumn()));
            ((System.ComponentModel.ISupportInitialize)(this.objectListView)).BeginInit();
            this.SuspendLayout();
            // 
            // objectListView
            // 
            this.objectListView.AllColumns.Add(this.olvColumnTitle);
            this.objectListView.AllColumns.Add(this.olvColumnValue);
            this.objectListView.CellEditActivation = BrightIdeasSoftware.ObjectListView.CellEditActivateMode.SingleClick;
            this.objectListView.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.olvColumnTitle,
            this.olvColumnValue});
            this.objectListView.CopySelectionOnControlC = false;
            this.objectListView.Dock = System.Windows.Forms.DockStyle.Fill;
            this.objectListView.EmptyListMsg = "No properties found";
            this.objectListView.FullRowSelect = true;
            this.objectListView.GridLines = true;
            this.objectListView.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.Nonclickable;
            this.objectListView.HideSelection = false;
            this.objectListView.Location = new System.Drawing.Point(0, 0);
            this.objectListView.MultiSelect = false;
            this.objectListView.Name = "objectListView";
            this.objectListView.RowHeight = 20;
            this.objectListView.SelectAllOnControlA = false;
            this.objectListView.SelectColumnsOnRightClick = false;
            this.objectListView.SelectColumnsOnRightClickBehaviour = BrightIdeasSoftware.ObjectListView.ColumnSelectBehaviour.None;
            this.objectListView.ShowFilterMenuOnRightClick = false;
            this.objectListView.ShowGroups = false;
            this.objectListView.ShowSortIndicators = false;
            this.objectListView.Size = new System.Drawing.Size(488, 390);
            this.objectListView.TabIndex = 0;
            this.objectListView.UseCompatibleStateImageBehavior = false;
            this.objectListView.View = System.Windows.Forms.View.Details;
            this.objectListView.CellEditFinishing += new BrightIdeasSoftware.CellEditEventHandler(this.objectListView_CellEditFinishing);
            this.objectListView.CellEditStarting += new BrightIdeasSoftware.CellEditEventHandler(this.objectListView_CellEditStarting);
            this.objectListView.CellEditValidating += new BrightIdeasSoftware.CellEditEventHandler(this.objectListView_CellEditValidating);
            this.objectListView.KeyDown += new System.Windows.Forms.KeyEventHandler(this.objectListView_KeyDown);
            this.objectListView.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.objectListView_KeyPress);
            // 
            // olvColumnTitle
            // 
            this.olvColumnTitle.AspectName = "title";
            this.olvColumnTitle.Groupable = false;
            this.olvColumnTitle.IsEditable = false;
            this.olvColumnTitle.Text = "Title";
            this.olvColumnTitle.Width = 100;
            // 
            // olvColumnValue
            // 
            this.olvColumnValue.AspectName = "value";
            this.olvColumnValue.FillsFreeSpace = true;
            this.olvColumnValue.Groupable = false;
            this.olvColumnValue.Text = "Value";
            // 
            // PropertyGrid
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.objectListView);
            this.Name = "PropertyGrid";
            this.Size = new System.Drawing.Size(488, 390);
            ((System.ComponentModel.ISupportInitialize)(this.objectListView)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private BrightIdeasSoftware.ObjectListView objectListView;
        private BrightIdeasSoftware.OLVColumn olvColumnTitle;
        private BrightIdeasSoftware.OLVColumn olvColumnValue;
    }
}
