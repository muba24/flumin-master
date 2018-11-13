using NodeSystemLib2;
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

namespace Flumin {
    public partial class BufferViewForm : DockContent {

        List<BufferView> views = new List<BufferView>();

        public Graph Graph { get; private set; }

        public BufferViewForm() {
            InitializeComponent();
            DockAreas = DockAreas.DockBottom |
                        DockAreas.DockLeft |
                        DockAreas.DockRight |
                        DockAreas.DockTop |
                        DockAreas.Float;
        }

        public void UpdateGraph() {
            foreach (var view in views) view.UpdateDisplay();
        }

        public void FromGraph(Graph g) {
            views.Clear();
            flowLayoutPanelBars.Controls.Clear();

            foreach (var node in g.Nodes) {
                foreach (var port in node.InputPorts) {
                    var view = new BufferView {
                        Port = port,
                        Visible = true
                    };
                    view.UpdateDisplay();
                    views.Add(view);
                    flowLayoutPanelBars.Controls.Add(view);
                }
            }

            Graph = g;
            timerRefresh.Enabled = true;
        }

        private void timerRefresh_Tick(object sender, EventArgs e) {
            UpdateGraph();
        }
    }
}
