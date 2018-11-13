using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using WeifenLuo.WinFormsUI.Docking;
using CustomPropertyGrid;

namespace Flumin {
    public partial class MetricProperties : DockContent {

        CustomPropertyGrid.PropertyGrid grid;
        NodeSystemLib2.Generic.NodeAttributes.IAttributable attrObject;
        NodeSystemLib2.Graph graph;

        public void ShowForObject(NodeSystemLib2.Generic.NodeAttributes.IAttributable o) {
            if (graph != null) {
                graph.StatusChanged -= Parent_StatusChanged;
            }

            attrObject = o;
            if (attrObject is NodeSystemLib2.Node) {
                graph = ((NodeSystemLib2.Node)attrObject).Parent;
            } else if (attrObject is NodeSystemLib2.Graph) {
                graph = (NodeSystemLib2.Graph)attrObject;
            } else {
                graph = null;
            }

            LoadAttributes(o.Attributes);
            Show();
        }

        public MetricProperties() {
            InitializeComponent();
            GlobalSettings.Instance.SelectedObjectChanged += GlobalSettings_SelectedNodesChanged;

            DockAreas = DockAreas.DockBottom |
                        DockAreas.DockLeft |
                        DockAreas.DockRight |
                        DockAreas.DockTop |
                        DockAreas.Float;

            grid = new CustomPropertyGrid.PropertyGrid();
            grid.Dock = DockStyle.Fill;
            Controls.Add(grid);
        }

        private void GlobalSettings_SelectedNodesChanged(object sender, object e) {
            if (graph != null) {
                graph.StatusChanged -= Parent_StatusChanged;
                graph = null;
            }

            if (e is GraphNode) {
                var node = (e as GraphNode).Node;
                graph = node.Parent;
                graph.StatusChanged += Parent_StatusChanged;
                attrObject = node;
            } else if (e is NodeSystemLib2.Graph) {
                graph = (NodeSystemLib2.Graph)e;
                graph.StatusChanged += Parent_StatusChanged;
                attrObject = graph;
            } else {
                grid.Properties.Clear();
                return;
            }
            LoadAttributes(attrObject.Attributes);
        }

        private void Parent_StatusChanged(object sender, NodeSystemLib2.Graph.StatusChanagedEventArgs e) {
            BeginInvoke(new Action(() => {
                grid.UpdateRowEnabledState();
            }));
        }

        private void LoadAttributes(IEnumerable<NodeSystemLib2.Generic.NodeAttributes.NodeAttribute> attrs) {
            grid.Properties.Clear();
            grid.Freeze();

            foreach (var attr in attrs) {
                try {
                    var prop = PropertyRowNodeAttribute.FromAttribute(attr);
                    grid.Properties.Add(prop);
                } catch (NotImplementedException) {
                    GlobalSettings.Instance.UserLog.Add(new LogMessage(LogMessage.LogType.Warning, "PropertyGrid: attribute type not implemented: " + attr.GetType()));
                }
            }

            grid.Unfreeze();
        }
    }
}
