using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace NodeEditorLib {
    public partial class Form1 : Form {

        static EditorControl.PortDataType TypeInt = new EditorControl.PortDataType(
            Guid.NewGuid(), "Int", Color.LightGreen
        );

        static EditorControl.PortDataType TypeFloat = new EditorControl.PortDataType(
            Guid.NewGuid(), "Float", Color.LightSeaGreen
        );

        private class NodeOutInt : EditorControl.Node {
            public NodeOutInt() {
                AddPort(new EditorControl.OutputPort(this, "Out 1", TypeInt));
                AddPort(new EditorControl.OutputPort(this, "Out 2", TypeFloat));
            }
        }

        private class NodeInInt : EditorControl.Node {
            public NodeInInt() {
                AddPort(new EditorControl.InputPort(this, "In 1", TypeInt));
                AddPort(new EditorControl.InputPort(this, "In 2", TypeFloat));
            }
        }

        public Form1() {
            InitializeComponent();

            var node1 = new NodeOutInt() {
                Location = new Point(0, 0),
                //Size = new Size(50, 100),
                Title = "Knoten"
            };

            var node2 = new NodeInInt() {
                Location = new Point(200, 100),
                //Size = new Size(70, 80),
                Title = "Knoten 2"
            };

            var node3 = new NodeInInt() {
                Location = new Point(200, 200),
                //Size = new Size(50, 40),
                Title = "Knoten 3"
            };

            nodeEditorControl1.Nodes.Add(node1);
            nodeEditorControl1.Nodes.Add(node2);
            nodeEditorControl1.Nodes.Add(node3);

            nodeEditorControl1.SelectionChanged += EventHandler;
            nodeEditorControl1.LinkCreated += EventHandler;
            nodeEditorControl1.LinkDestroyed += EventHandler;
            nodeEditorControl1.NodeCollectionModified += EventHandler;
        }

        private void EventHandler(object sender, EventArgs e) {
            System.Diagnostics.Debug.WriteLine("Event: " + e.ToString());
        }

        private void nodeEditorControl1_KeyDown(object sender, KeyEventArgs e) {
            if (e.KeyCode == Keys.Delete) {
                var sel = nodeEditorControl1.Nodes.Where(n => n.Selected).ToArray();
                foreach (var node in sel) {
                    nodeEditorControl1.Nodes.Remove(node);
                }
            }
        }
    }
}
