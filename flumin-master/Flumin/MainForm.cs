using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Xml;
using Microsoft.VisualBasic;
using WeifenLuo.WinFormsUI.Docking;
using System.Threading;
//using SimpleADC.Metrics;

namespace Flumin {

    public partial class MainForm : Form {

        public MainForm() {
            InitializeComponent();
            //NodeSystemLib.NodeSystemSettings.Instance.SystemHost = GlobalSettings.Instance.AsNodeSystemHost();
            FillMetrics();
        }

        private void FillMetrics() {
            //string metricPath = @"C:\Users\arne\Documents\Visual Studio 2015\Projects\WaveControlTest\MetricLibs";
            //if (Directory.Exists(metricPath)) {
            //    GlobalSettings.Instance.MetricManager.FindMetrics(metricPath);
            //}

            var metricPath = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "MetricLibs");
            if (Directory.Exists(metricPath)) {
                GlobalSettings.Instance.MetricManager.FindMetrics(metricPath);
            }

            var args = Environment.GetCommandLineArgs();
            if (args.Length > 1) {
                GlobalSettings.Instance.MetricManager.FindMetrics(args[1]);
            }

            //GlobalSettings.Instance.MetricManager.TryRegisterMetric(typeof(MetricStop));
            //GlobalSettings.Instance.MetricManager.TryRegisterMetric(typeof(MetricThreshold));
            //GlobalSettings.Instance.MetricManager.TryRegisterMetric(typeof(MetricThresholdEvent));
            GlobalSettings.Instance.MetricManager.TryRegisterMetric(typeof(Metrics.MetricBuffer));
            //GlobalSettings.Instance.MetricManager.TryRegisterMetric(typeof(MetricValueSink));
            //GlobalSettings.Instance.MetricManager.TryRegisterMetric(typeof(MetricLogicNot));
            //GlobalSettings.Instance.MetricManager.TryRegisterMetric(typeof(MetricTimeStampSink));
            GlobalSettings.Instance.MetricManager.TryRegisterMetric(typeof(Metrics.MetricSustainedThreshold));
            GlobalSettings.Instance.MetricManager.TryRegisterMetric(typeof(Metrics.MetricThresholdHystersis));
            GlobalSettings.Instance.MetricManager.TryRegisterMetric(typeof(Metrics.MetricFile));
            GlobalSettings.Instance.MetricManager.TryRegisterMetric(typeof(Metrics.MetricValueTimeDelay));
        }

        private GraphNode GraphNodeFromNode(NodeEditor e, NodeSystemLib2.Node n) {
            foreach (var ne in e.NodeView.Nodes.OfType<GraphNode>()) {
                if (ne.Node == n) {
                    return ne;
                }
            }
            return null;
        }

        private void SaveNodeEditor(NodeEditor editor, XmlWriter writer) {
            writer.WriteStartElement("editor");
            {
                writer.WriteAttributeString("active", (editor == GlobalSettings.Instance.ActiveEditor) ? "1" : "0");
                writer.WriteAttributeString("title", editor.Text);
                writer.WriteAttributeString("x", editor.NodeView.Center.X.ToString());
                writer.WriteAttributeString("y", editor.NodeView.Center.Y.ToString());
                writer.WriteAttributeString("zoom", editor.NodeView.Zoom.ToString(CultureInfo.InvariantCulture));
                //writer.WriteAttributeString("clocksync", GraphNodeFromNode(editor, editor.Graph.ClockSynchronizationNode)?.InstanceId.ToString());

                GlobalSettings.Instance.MetricManager.SaveFactorySettings(editor.Graph, writer);

                writer.WriteStartElement("graphnodes");
                {
                    foreach (var node in editor.NodeView.Nodes.OfType<GraphNode>()) {
                        writer.WriteStartElement("graphnode");
                        {
                            node.Serialize(writer, editor);
                        }
                        writer.WriteEndElement();
                    }
                }
                writer.WriteEndElement();

                writer.WriteStartElement("links");
                {
                    foreach (var node in editor.NodeView.Nodes) {
                        foreach (var port in node.InputPorts.Where(p => p.Connection != null)) {
                            writer.WriteStartElement("link");
                            writer.WriteAttributeString("targetOwnerId", ((GraphNode)port.Connection.Parent).InstanceId.ToString());
                            writer.WriteAttributeString("targetInputName", port.Connection.Name);
                            writer.WriteAttributeString("sourceOwnerId", ((GraphNode)port.Parent).InstanceId.ToString());
                            writer.WriteAttributeString("sourceOutputName", port.Name);
                            writer.WriteEndElement();
                        }
                    }
                }
                writer.WriteEndElement();

            }
            writer.WriteEndElement();
        }

        private void SaveSingleNodeEditor(NodeEditor editor, string file) {
            var writer = XmlWriter.Create(file);
            SaveNodeEditor(editor, writer);
            writer.Close();
        }

        private void SaveAllNodeEditors(string file) {
            var writer = XmlWriter.Create(file);

            writer.WriteStartDocument();
            writer.WriteStartElement("editors");
            {
                foreach (var editor in _dock.Documents.OfType<NodeEditor>()) {
                    SaveNodeEditor(editor, writer);
                }
            }
            writer.WriteEndElement();
            writer.Close();
        }

        private struct NodeEditorInfo {
            public NodeEditor Editor;
            public bool Active;
        }

        private NodeEditorInfo LoadNodeEditor(XmlNode editor, int number) {
            var instances = new Dictionary<string, GraphNode>();
            
            var nodes = editor.SelectNodes("graphnodes/graphnode");
            if (nodes == null) return new NodeEditorInfo { Editor = null, Active = false };

            var nodeEditor = new NodeEditor();

            var factorySettings = editor.SelectNodes("factory");
            foreach (var setting in factorySettings.OfType<XmlNode>()) {
                var fid = Guid.Parse(setting.TryGetAttribute("guid", Guid.Empty.ToString()));
                var fac = GlobalSettings.Instance.MetricManager.Factories.FirstOrDefault(f => f.Id.Equals(fid));

                if (fac != null) {
                    fac.SetFactorySettings(nodeEditor.Graph, setting);
                }
            }

            var zoom = float.Parse(editor.TryGetAttribute("zoom", otherwise: "1"), CultureInfo.InvariantCulture);

            var xy = new System.Drawing.Point(
                int.Parse(editor.TryGetAttribute("x", otherwise: "0")),
                int.Parse(editor.TryGetAttribute("y", otherwise: "0"))
            );

            nodeEditor.NodeView.Center = xy;

            nodeEditor.Text = editor.TryGetAttribute("title", otherwise: $"Editor {number}");

            var clockSyncGuidString = editor.TryGetAttribute("clocksync", otherwise: "");
            if (clockSyncGuidString == "") clockSyncGuidString = Guid.Empty.ToString();
            var clockSyncId = Guid.Parse(clockSyncGuidString);

            var activeEditor = (editor.TryGetAttribute("active", otherwise: "0") == "1");

            foreach (XmlNode node in nodes) {
                var formNode = GraphNode.FromXml(node, nodeEditor.Graph);
                if (formNode == null) continue;
                nodeEditor.NodeView.Nodes.Add(formNode);
                instances.Add(formNode.InstanceId.ToString(), formNode);

                //if (formNode.InstanceId.Equals(clockSyncId)) {
                //    nodeEditor.Graph.ClockSynchronizationNode = formNode.Node;
                //}
            }

            nodeEditor.OnRunningStatusChanged += Editor_OnRunningStatusChanged;
            nodeEditor.Show(GlobalSettings.Instance.DockPanelInstance, DockState.Document);

            var links = editor.SelectNodes("links/link");
            if (links == null) return new NodeEditorInfo { Editor = nodeEditor, Active = activeEditor };

            foreach (XmlNode link in links) {
                if (link.Attributes == null)
                    continue;

                var targetOwnerId = link.TryGetAttribute("targetOwnerId", otherwise: "");
                var sourceOwnerId = link.TryGetAttribute("sourceOwnerId", otherwise: "");
                var targetName   = link.TryGetAttribute("targetInputName", otherwise: "");
                var sourceName   = link.TryGetAttribute("sourceOutputName", otherwise: "");

                if (targetOwnerId == "" || sourceOwnerId == "" || targetName == "" || sourceName == "")
                    continue;

                if (instances.ContainsKey(sourceOwnerId) && instances.ContainsKey(targetOwnerId)) {
                    var pIn = instances[sourceOwnerId].InputPorts.First(c => c.Name == sourceName);
                    var pOut = instances[targetOwnerId].OutputPorts.First(c => c.Name == targetName);
                    nodeEditor.NodeView.CreateConnection(pIn, pOut);
                }
            }

            return new NodeEditorInfo { Editor = nodeEditor, Active = activeEditor };
        }

        private void LoadSingleNodeEditor(string file) {
            var doc = new XmlDocument();

            if (!File.Exists(file)) return;
            doc.Load(file);

            var editor = doc.SelectNodes("//editor").Item(0);
            if (editor == null) return;

            LoadNodeEditor(editor, 0);
        }
        
        private int LoadNodeEditors(string file) {
            var doc = new XmlDocument();

            NodeEditor activeEditor = null;
            var editorCount = 0;

            if (!File.Exists(file)) return editorCount;
            doc.Load(file);

            var editors = doc.SelectNodes("//editors/editor");
            if (editors == null) return editorCount;

            foreach (XmlNode editor in editors) {
                var editorInfo = LoadNodeEditor(editor, editorCount);
                if (editorInfo.Editor != null) {
                    if (editorInfo.Active) activeEditor = editorInfo.Editor;
                    editorCount++;
                }
            }

            if (editorCount > 0) {
                _dock.Documents.FirstOrDefault(d => d.DockHandler.Content == activeEditor)?.DockHandler.Activate();
            }

            return editorCount;
        }

        private void _dock_ActiveDocumentChanged(object sender, EventArgs e) {
            if (_dock.ActiveDocument is NodeEditor) {
                GlobalSettings.Instance.ActiveEditor = (NodeEditor) _dock.ActiveDocument;
                toolStripButtonStart.Enabled = !GlobalSettings.Instance.ActiveEditor.GraphRunning;
                toolStripButtonStop.Enabled  =  GlobalSettings.Instance.ActiveEditor.GraphRunning;
            }
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e) {
            GlobalSettings.Instance.StopProcessing(asynchronous: false);
            var localPath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            SaveAllNodeEditors(System.IO.Path.Combine(localPath, "nodes.xml"));
        }

        private void toolStripButton1_Click(object sender, EventArgs e) {
            CreateNewNodeEditor();
        }

        private void CreateNewNodeEditor() {
            var editorPanel = new NodeEditor();
            editorPanel.OnRunningStatusChanged += Editor_OnRunningStatusChanged;
            editorPanel.Show(_dock, DockState.Document);
        }

        private void Editor_OnRunningStatusChanged(object sender, bool e) {
            if (sender == GlobalSettings.Instance.ActiveEditor) {
                var action = new Action(() => {
                    toolStripButtonStart.Enabled = !GlobalSettings.Instance.ActiveEditor.GraphRunning;
                    toolStripButtonStop.Enabled = GlobalSettings.Instance.ActiveEditor.GraphRunning;
                });

                // TODO: Potential deadlock.
                // If another thread calls StopGraph this will be called.
                if (InvokeRequired) {
                    BeginInvoke(action);
                } else {
                    action();
                }
            }
        }

        private void toolStripButtonClose_Click(object sender, EventArgs e) {
            _dock.ActiveDocument?.DockHandler.Close();
        }

        private void toolStripButtonRename_Click(object sender, EventArgs e) {
            var form = _dock.ActiveDocument as Form;
            if (form == null) return;
            var input = Interaction.InputBox("Neuer Name für Tab:", "Neuer Name", form.Text);
            if (input != "") {
                form.Text = input;
            }
        }

        private void MainForm_Load(object sender, EventArgs e) {
            FormClosing += MainForm_FormClosing;

            GlobalSettings.Instance.DockPanelInstance = _dock;

            _dock.SuspendLayout(true);

            var toolboxPanel = new Tools();
            toolboxPanel.Show(_dock, DockState.DockLeft);

            var errListPanel = new UserLogList();
            errListPanel.Show(_dock, DockState.DockBottom);

            var propertiesPanel = new MetricProperties();
            propertiesPanel.Show(_dock, DockState.DockRight);

            var pane = propertiesPanel.DockHandler.Pane;

            var recordingsPanel = new RecordInfoViewTree(); //RecordInfoView();
            recordingsPanel.Show(pane, DockAlignment.Bottom, 0.5);

            var localPath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            if (LoadNodeEditors(System.IO.Path.Combine(localPath, "nodes.xml")) == 0) {   
                CreateNewNodeEditor();
            }

            _dock.ActiveDocumentChanged += _dock_ActiveDocumentChanged;

            _dock.ResumeLayout(true, true);

            toolboxPanel.DockPanel.DockLeftPortion = 0.1;
        }

        private void toolStripButtonStart_Click(object sender, EventArgs e) {
            if (!(_dock.ActiveDocument is NodeEditor)) return;

            GlobalSettings.Instance.StopProcessing(asynchronous: false);

            if (_dock.ActiveDocument != null) {
                ((NodeEditor) _dock.ActiveDocument).RunGraph();
            }
        }

        private void toolStripButtonStop_Click(object sender, EventArgs e) {
            GlobalSettings.Instance.StopProcessing(asynchronous: false);
            toolStripButtonStart.Enabled = true;
            toolStripButtonStop.Enabled = false;
        }

        private void menuStrip1_ItemClicked(object sender, ToolStripItemClickedEventArgs e) {

        }

        private void debugToolStripMenuItem_Click(object sender, EventArgs e) {

        }

        private void timerThreadWatch_Tick(object sender, EventArgs e) {
            toolStripStatusLabelThreads.Text = $"Pending operations: Not able to measure";
        }

        private void toolStripButtonSaveEditor_Click(object sender, EventArgs e) {
            var editor = _dock.ActiveDocument as NodeEditor;
            if (editor == null) return;

            saveFileDialogXml.FileName = editor.Text + ".xml";
            var result = saveFileDialogXml.ShowDialog();
            if (result == DialogResult.OK) {
                SaveSingleNodeEditor(editor, saveFileDialogXml.FileName);
            }
        }

        private void toolStripButtonOpenXml_Click(object sender, EventArgs e) {
            var result = openFileDialogXml.ShowDialog();
            if (result == DialogResult.OK) {
                foreach (var filename in openFileDialogXml.FileNames) {
                    LoadSingleNodeEditor(filename);
                }
            }
        }

        private void toolStripButton4_Click(object sender, EventArgs e) {
            var frm = new BufferViewForm();
            frm.Show(GlobalSettings.Instance.DockPanelInstance, DockState.DockRight);
            var graph = GlobalSettings.Instance.ActiveEditor.Graph;
            frm.FromGraph(graph);
        }
    }
}
