namespace Flumin {
    partial class NodeEditor {
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
            this.contextMenuStrip = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.contextMenuStripProperties = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.eigenschaftenToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.clockSyncToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolTipNodeInfo = new System.Windows.Forms.ToolTip(this.components);
            this.nodeGraphPanel = new NodeEditorLib.EditorControl.NodeEditorControl();
            this.contextMenuStripProperties.SuspendLayout();
            this.SuspendLayout();
            // 
            // contextMenuStrip
            // 
            this.contextMenuStrip.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.contextMenuStrip.Name = "contextMenuStrip";
            this.contextMenuStrip.Size = new System.Drawing.Size(61, 4);
            // 
            // contextMenuStripProperties
            // 
            this.contextMenuStripProperties.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.contextMenuStripProperties.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.eigenschaftenToolStripMenuItem,
            this.clockSyncToolStripMenuItem});
            this.contextMenuStripProperties.Name = "contextMenuStripProperties";
            this.contextMenuStripProperties.Size = new System.Drawing.Size(171, 52);
            // 
            // eigenschaftenToolStripMenuItem
            // 
            this.eigenschaftenToolStripMenuItem.Name = "eigenschaftenToolStripMenuItem";
            this.eigenschaftenToolStripMenuItem.Size = new System.Drawing.Size(170, 24);
            this.eigenschaftenToolStripMenuItem.Text = "Eigenschaften";
            // 
            // clockSyncToolStripMenuItem
            // 
            this.clockSyncToolStripMenuItem.Name = "clockSyncToolStripMenuItem";
            this.clockSyncToolStripMenuItem.Size = new System.Drawing.Size(170, 24);
            this.clockSyncToolStripMenuItem.Text = "Clock Sync";
            // 
            // nodeGraphPanel
            // 
            this.nodeGraphPanel.AllowDrop = true;
            this.nodeGraphPanel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(56)))), ((int)(((byte)(71)))), ((int)(((byte)(84)))));
            this.nodeGraphPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.nodeGraphPanel.Location = new System.Drawing.Point(0, 0);
            this.nodeGraphPanel.Name = "nodeGraphPanel";
            this.nodeGraphPanel.Size = new System.Drawing.Size(539, 363);
            this.nodeGraphPanel.TabIndex = 0;
            this.nodeGraphPanel.Zoom = 1F;
            this.nodeGraphPanel.Load += new System.EventHandler(this.nodeGraphPanel_Load);
            this.nodeGraphPanel.DragDrop += new System.Windows.Forms.DragEventHandler(this.nodeGraphPanel_DragDrop);
            this.nodeGraphPanel.DragEnter += new System.Windows.Forms.DragEventHandler(this.nodeGraphPanel_DragEnter);
            this.nodeGraphPanel.KeyDown += new System.Windows.Forms.KeyEventHandler(this.nodeGraphPanel_KeyDown);
            this.nodeGraphPanel.MouseDown += new System.Windows.Forms.MouseEventHandler(this.nodeGraphPanel_MouseDown);
            // 
            // NodeEditor
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 17F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(539, 363);
            this.Controls.Add(this.nodeGraphPanel);
            this.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Name = "NodeEditor";
            this.Text = "NodeEditor";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.NodeEditor_FormClosing);
            this.contextMenuStripProperties.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private NodeEditorLib.EditorControl.NodeEditorControl nodeGraphPanel;
        private System.Windows.Forms.ContextMenuStrip contextMenuStrip;
        private System.Windows.Forms.ContextMenuStrip contextMenuStripProperties;
        private System.Windows.Forms.ToolStripMenuItem eigenschaftenToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem clockSyncToolStripMenuItem;
        private System.Windows.Forms.ToolTip toolTipNodeInfo;
    }
}