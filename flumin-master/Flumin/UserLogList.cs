using System;
using System.Collections.Specialized;
using System.Windows.Forms;
using WeifenLuo.WinFormsUI.Docking;
using System.Linq;

namespace Flumin {
    public partial class UserLogList : DockContent {

        enum ImageIndex {
            Error = 0,
            Warning = 1,
            Info = 2
        }

        System.Collections.Generic.Dictionary<LogMessage.LogType, ImageIndex> ImageLookup = new System.Collections.Generic.Dictionary<LogMessage.LogType, ImageIndex>() {
            { LogMessage.LogType.Error, ImageIndex.Error },
            { LogMessage.LogType.Warning, ImageIndex.Warning },
            { LogMessage.LogType.Info, ImageIndex.Info }
        };

        object _hasChangedLock = new object();
        volatile bool _hasChanged;

        public UserLogList() {
            InitializeComponent();
            GlobalSettings.Instance.UserLog.CollectionChanged += Errors_CollectionChanged;
            DockAreas = DockAreas.DockBottom |
                        DockAreas.DockLeft |
                        DockAreas.DockRight |
                        DockAreas.DockTop |
                        DockAreas.Float;
        }

        private void Errors_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e) {
            lock (_hasChangedLock) {
                _hasChanged = true;
            }
        }

        private void ReloadErrorList() {
            lock (_hasChangedLock) {
                if (!_hasChanged) return;
                _hasChanged = false;
            }

            listViewErrors.Items.Clear();

            foreach (var log in GlobalSettings.Instance.UserLog.ToSyncArray()) {
                var item = listViewErrors.Items.Add(log.Date.ToLongTimeString());

                if (log is NodeMessage) {
                    item.SubItems.Add(((NodeMessage)log).Node.GetQualifier());
                } else if (log is GraphMessage) {
                    item.SubItems.Add("Graph");
                } else if (log is FormMessage) {
                    item.SubItems.Add(((FormMessage)log).Form.Text);
                } else {
                    item.SubItems.Add("");
                }

                item.SubItems.Add(log.Description);
                item.ImageIndex = (int)ImageLookup[log.Type];
            }
        }

        private void timerRefresh_Tick(object sender, EventArgs e) {
            ReloadErrorList();
        }

        private void listViewErrors_SelectedIndexChanged(object sender, EventArgs e) {

        }

        private void listViewErrors_MouseDoubleClick(object sender, MouseEventArgs e) {
            if (listViewErrors.SelectedIndices.Count <= 0) return;

            var error = GlobalSettings.Instance.UserLog[listViewErrors.SelectedIndices[0]];

            if (error is NodeMessage) {
                var node = ((NodeMessage)error).Node;

                foreach (var dock in GlobalSettings.Instance.DockPanelInstance.Documents.OfType<NodeEditor>()) {
                    foreach (var graphNode in dock.NodeView.Nodes.OfType<GraphNode>()) {
                        if (graphNode.Node == node) {
                            dock.DockHandler.Activate();
                            dock.SelectNode(graphNode);
                            dock.Refresh();
                            return;
                        }
                    }
                }
            }
        }

        private void clearToolStripMenuItem_Click(object sender, EventArgs e) {
            GlobalSettings.Instance.UserLog.Clear();
            ReloadErrorList();
        }
    }
}
