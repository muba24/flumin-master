using System;
using System.Drawing;
using System.Windows.Forms;
using System.Linq;
using System.Xml;
using DeviceLibrary;
using NodeSystemLib2;
using WeifenLuo.WinFormsUI.Docking;
using NodeSystemLib2.Generic;
using EC = NodeEditorLib.EditorControl;
using System.Collections.Generic;

namespace Flumin {
    public partial class NodeEditor : DockContent {

        /// <summary>
        /// Position im Fenster, an der ein Rechtsklick gemacht wurde.
        /// Wichtig, damit ein neuer Node, der aus dem Kontextmenü heraus erzeugt wurde,
        /// an der ursprünglichen Mausstelle erscheint.
        /// </summary>
        private Point RightClickPosition { get; set; }

        /// <summary>
        /// Wird ausgelöst, wenn der Graph gestartet/gestoppt wird.
        /// Listener ist MainForm, um Start/Stop Buttons zu (de)aktivieren
        /// </summary>
        public event EventHandler<bool> OnRunningStatusChanged;

        ///// <summary>
        ///// Für Serialisierung durch MainForm
        ///// </summary>
        //public NodeGraphView NodeView => nodeGraphPanel.View;

        /// <summary>
        /// Eigentliche Graph Verwaltung
        /// </summary>
        private readonly Graph _graph = new Graph();

        public EC.NodeEditorControl NodeView => nodeGraphPanel;


        /// <summary>
        /// 
        /// </summary>
        public NodeEditor() {
            InitializeComponent();

            nodeGraphPanel.DoubleClick += NodeGraphPanelDoubleClick;
            nodeGraphPanel.SelectionChanged += NodeGraphPanel_SelectionChanged;
            nodeGraphPanel.LinkCreated += NodeGraphPanel_LinkCreated;
            nodeGraphPanel.LinkDestroyed += NodeGraphPanel_LinkDestroyed;
            nodeGraphPanel.NodeCollectionModified += NodeGraphPanel_NodeCollectionModified;
            FillContextMenu();

            _graph.Context = new GraphContext() {
                FileMask = @"%name%.%ext%",
                WorkingDirectoryMask = @"C:\tmp\%date%\set"
            };

            _graph.StatusChanged += _graph_StatusChanged;
            _graph.Disconnected += _graph_Disconnected;
            _graph.Connected += _graph_Connected;
        }

        private void NodeGraphPanel_NodeCollectionModified(object sender, EC.NodeCollectionModifiedEventArgs e) {
            if (e.Action == NodeEditorLib.EditorControl.NodeCollectionModifiedEventArgs.ActionType.Removed) {
                var node = ((GraphNode) e.Item).Node;
                Graph.RemoveNode(node);
                node.Dispose();
            }
        }

        private void _graph_Connected(object sender, Graph.ConnectedEventArgs e) {
            bool found = false;

            for (int i = 0; i < nodeGraphPanel.Nodes.Count; i++) {
                foreach (var output in nodeGraphPanel.Nodes[i].OutputPorts.OfType<OutputGraphConnector>()) {
                    foreach (var target in output.Ports) {
                        if (output.Port.Connections.Contains(e.Port)) {
                            found = true;
                        }
                    }
                }
            }

            if (!found) {
                // find node containing e
                var nodeInput = nodeGraphPanel.Nodes.OfType<GraphNode>().First(n => n.Node == e.Port.Parent);
                var nodeOutput = nodeGraphPanel.Nodes.OfType<GraphNode>().First(n => n.Node == e.Port.Connection.Parent);

                // find input port
                var conInput = nodeInput.InputPorts.OfType<InputGraphConnector>().First(i => i.Port.Name == e.Port.Name);
                var conOutput = nodeOutput.OutputPorts.OfType<OutputGraphConnector>().First(i => i.Port.Name == e.Port.Connection.Name);

                conInput.Connection = conOutput;
            }
        }

        private void _graph_Disconnected(object sender, Graph.DisconnectedEventArgs e) {
            foreach (var node in NodeView.Nodes) {
                foreach (var input in node.InputPorts.OfType<InputGraphConnector>()) {
                    if (input.Port == e.Port) {
                        input.Connection = null;
                    }
                }
            }
        }

        private void _graph_StatusChanged(object sender, Graph.StatusChanagedEventArgs e) {
            if (e.NewState == Graph.State.Stopped) {
                // can't be sure if graph wasn't stopped asynchronously,
                // so always call CleanupAsyncStop when graph was stopped.
                BeginInvoke(new Action(()=> {
                    Graph.CleanupAsyncStop();
                }));
            }

            OnRunningStatusChanged?.Invoke(this, e.NewState == Graph.State.Running);
        }

        /// <summary>
        /// Rechtsklickmenü für Node Editor
        /// </summary>
        private void FillContextMenu() {
            //
        }

        /// <summary>
        /// Ob die Devices im Graph aktiv sind.
        /// Kann nur von NodeEditor verändert werden, löst OnRunningStatusChanged aus.
        /// </summary>
        public bool GraphRunning {
            get {
                return _graph.GraphState == Graph.State.Running;
            }
        }

        /// <summary>
        /// Graph muss öffentlich sein, damit MainForm Nodes aus der XML Datei wiederherstellen kann
        /// </summary>
        public Graph Graph => _graph;

        /// <summary>
        /// Doppelklick auf Graph, erkennt ob Doppelklick auf Node gemacht wurde
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void NodeGraphPanelDoubleClick(object sender, EventArgs e) {
            var position  = nodeGraphPanel.PointToClient(Cursor.Position);
            var graphNode = nodeGraphPanel.NodeFromPoint(position);
            var node      = (graphNode as GraphNode)?.Node;
            (node as NodeSystemLib2.Generic.INodeUi)?.OnDoubleClick();
        }

        /// <summary>
        /// Rechtsklick Unterstützung für Nodes,
        /// sonst Kontextmenü mit Devices und Metriken
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void nodeGraphPanel_MouseDown(object sender, MouseEventArgs e) {
            if (e.Button != MouseButtons.Right) return;
            RightClickPosition = e.Location;

            var selNode = nodeGraphPanel.NodeFromPoint(e.Location);
            if (selNode != null) {
                nodeGraphPanel.SelectNode(selNode);
                nodeGraphPanel.Invalidate();
                contextMenuStripProperties.Show(nodeGraphPanel, e.Location);
            } else {
                contextMenuStrip.Show(nodeGraphPanel, e.Location);
            }
        }

        private void AddMetric(Point position, MetricManager.MetricInfo metricInfo) {
            var viewPosition = nodeGraphPanel.ControlToWorld(position);

            if (GraphRunning) {
                if (AskStopGraph()) StopGraph();
                else return;
            }

            if (metricInfo != null) {
                NodeSystemLib2.Node metric = null;

                try {
                    metric = metricInfo.CreateInstance(_graph);
                } catch (Exception e) {
                    GlobalSettings.Instance.UserLog.Add(new GraphMessage(Graph, LogMessage.LogType.Error, $"In AddMetric for {metricInfo.Name}: {e}"));
                    return;
                }

                var graphNode = new GraphNode(
                    node:           metric,
                    factoryId:      metricInfo.FactoryGuid,
                    uniqueName:     metricInfo.UniqueName,
                    pX:             viewPosition.X,
                    pY:             viewPosition.Y
                );
                nodeGraphPanel.Nodes.Add(graphNode);
            } else {
                throw new ArgumentNullException();
            }

        }

        private bool AskStopGraph() {
            var result = MessageBox.Show(
                this, 
                "Graph is currently running.\nNew nodes can only be added in idle mode.\nStop graph?", "Stop graph?", 
                MessageBoxButtons.YesNo, 
                MessageBoxIcon.Question
            );

            return result == DialogResult.Yes;
        }

        /// <summary>
        /// Sich schließender Node Editor muss Graph stoppen,
        /// da sonst Threads nicht enden
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void NodeEditor_FormClosing(object sender, FormClosingEventArgs e) {
            if (GraphRunning) StopGraph();
            //Graph.Dispose();
        }

        /// <summary>
        /// Um den Graph zu starten, müssen erstmal alle Node Threads gestartet werden.
        /// Die Device Nodes markieren die Device Ports als aktiv.
        /// Erst dann ist Port.Status == Active. Danach können die Devices gestartet werden.
        /// </summary>
        /// <returns></returns>
        public bool RunGraph() {
            try {
                _graph.Start();
                return true;
            } catch (NodeException e) {
                GlobalSettings.Instance.UserLog.Add(new NodeMessage(e.Node, LogMessage.LogType.Error, e.Exception.Message));
                return false;
            } catch (Exception e) {
                GlobalSettings.Instance.UserLog.Add(new LogMessage(LogMessage.LogType.Error, e.Message));
                return false;
            }
        }

        /// <summary>
        /// Graph wird genau in umgekehrter Reihenfolge von RunGraph gestoppt.
        /// </summary>
        public void StopGraph() {
            _graph.Stop();
        }

        /// <summary>
        /// Markierte Nodes global dem ganzen Programm mitteilen.
        /// Andere Fenster können so damit arbeiten.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void NodeGraphPanel_SelectionChanged(object sender, EC.SelectionChangedEventArgs e) {
            if (nodeGraphPanel.SelectedNodes == null || !nodeGraphPanel.SelectedNodes.Any()) {
                GlobalSettings.Instance.SetSelectedObject(this, _graph);
            } else {
                GlobalSettings.Instance.SetSelectedObject(this, nodeGraphPanel.SelectedNodes.FirstOrDefault());
            }
        }

        public void SelectNode(GraphNode node) {
            if (!NodeView.Nodes.Contains(node)) {
                throw new InvalidOperationException("Node not found in this graph");
            }
            nodeGraphPanel.SelectNode(node);
        }

        /// <summary>
        /// Link in Graph gezogen, unterliegende Node Ports auch miteinander verbinden
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void NodeGraphPanel_LinkCreated(object sender, EC.LinkEventArgs e) {
            var from = (OutputPort) ((OutputGraphConnector) e.PortOut).Port;
            var to   = (InputPort)  ((InputGraphConnector) e.PortIn).Port;
            @from.AddConnection(to);
            to.Connection = @from;
        }

        /// <summary>
        /// Link aus Graph entfernt, unterliegende Node Ports auseinanderreissen
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void NodeGraphPanel_LinkDestroyed(object sender, EC.LinkEventArgs e) {
            var from = (OutputPort)((OutputGraphConnector)e.PortOut).Port;
            var to   = (InputPort) ((InputGraphConnector)e.PortIn).Port;
            @from.RemoveConnection(to);
            to.Connection = null;
        }

        private void nodeGraphPanel_DragEnter(object sender, DragEventArgs e) {
            if (e.Data.GetDataPresent(typeof(Silver.UI.ToolBoxItem))) {
                e.Effect = DragDropEffects.Copy;
                var obj = (Silver.UI.ToolBoxItem)e.Data.GetData(typeof(Silver.UI.ToolBoxItem));
                if (obj.Object is Type) {
                    System.Diagnostics.Debug.WriteLine((obj.Object as MetricManager.MetricInfo).Name);
                }
            }
        }

        private void nodeGraphPanel_DragDrop(object sender, DragEventArgs e) {
            if (e.Data.GetDataPresent(typeof(Silver.UI.ToolBoxItem))) {
                var obj   = (Silver.UI.ToolBoxItem)e.Data.GetData(typeof(Silver.UI.ToolBoxItem));
                var point = nodeGraphPanel.PointToClient(new Point(e.X, e.Y));
                AddMetric(point, obj.Object as MetricManager.MetricInfo);
            }
        }

        //private void clockSyncToolStripMenuItem_Click(object sender, EventArgs e) {
        //    var node = (GraphNode)nodeGraphPanel.View.SelectedItems.FirstOrDefault();
        //    if (node == null) return;

        //    if (node.InputConnectors.Any()) {
        //        MessageBox.Show("Selected node is not an input!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        //    }

        //    //_graph.ClockSynchronizationNode = node.Node;
        //}

        private void nodeGraphPanel_KeyDown(object sender, KeyEventArgs e) {
            Func<EC.Node, Rectangle> ToRect = n => n.Area;

            if (e.KeyCode == Keys.Delete) {
                var nodes = NodeView.Nodes.Where(n => n.Selected).ToArray();
                foreach (var node in nodes) {
                    NodeView.Nodes.Remove(node);
                }
            }

            if (e.KeyCode == Keys.Space && nodeGraphPanel.Nodes.Count > 0) {
                var rc     = ToRect(nodeGraphPanel.Nodes.First());
                var top    = rc.Top;
                var left   = rc.Left;
                var right  = rc.Right;
                var bottom = rc.Bottom;

                foreach (var node in nodeGraphPanel.Nodes) {
                    var nrc = ToRect(node);
                    left    = Math.Min(left,    nrc.Left);
                    right   = Math.Max(right,   nrc.Right);
                    top     = Math.Min(top,     nrc.Top);
                    bottom  = Math.Max(bottom,  nrc.Bottom);
                }

                var newView = new Rectangle(left, top, right - left, bottom - top);
                var newZoom = Math.Min((float)nodeGraphPanel.Height / newView.Height, (float)nodeGraphPanel.Width / newView.Width);

                nodeGraphPanel.Zoom = newZoom - 0.1f;
                nodeGraphPanel.Center = new Point(newView.Left + newView.Width / 2, newView.Top + newView.Height / 2);

                nodeGraphPanel.Refresh();
            }
        }

        //private void nodeGraphPanel_OnConnectorMouseEnter(object sender, NodeGraphPanelConnectorMouseEnterArgs args) {
        //    var connectorArea = args.Connector.GetHitArea();
        //    var point         = new Point(connectorArea.Right, connectorArea.Bottom);

        //    ShowConnectorToolTip(args.Connector, point);
        //}

        private void ShowConnectorToolTip(EC.Port connector, Point point) {
            var parentNode     = connector.Parent;
            var graphNode      = parentNode as GraphNode;
            var port = (connector is InputGraphConnector) ? (Port)((InputGraphConnector)connector).Port : ((OutputGraphConnector)connector).Port;

            if (graphNode != null) {
                var connectorTypeName = port.DataType.Description;
                var additional = "";

                if (port is NodeSystemLib2.FormatData1D.InputPortData1D) {
                    var port1d = (NodeSystemLib2.FormatData1D.InputPortData1D)port;
                    additional = $"Samplerate: {port1d.Samplerate}\nQueue capacity: {port1d.Capacity}\nBuffer capacity: {port1d.BufferCapacity}";
                }

                if (port is NodeSystemLib2.FormatData1D.OutputPortData1D) {
                    var port1d = (NodeSystemLib2.FormatData1D.OutputPortData1D)port;
                    additional = $"Samplerate: {port1d.Samplerate}\nQueue capacity: {port1d.Buffer?.Capacity ?? 0}";
                }

                if (port is NodeSystemLib2.FormatDataFFT.InputPortDataFFT) {
                    var port1d = (NodeSystemLib2.FormatDataFFT.InputPortDataFFT)port;
                    additional = $"Samplerate: {port1d.Samplerate}\nFFT size: {port1d.FFTSize}\nQueue frame capacity: {port1d.Capacity}\nBuffer frame capacity: {port1d.BufferCapacity}";
                }

                if (port is NodeSystemLib2.FormatDataFFT.OutputPortDataFFT) {
                    var port1d = (NodeSystemLib2.FormatDataFFT.OutputPortDataFFT)port;
                    additional = $"Samplerate: {port1d.Samplerate}\nFFT size: {port1d.FFTSize}\nQueue frame capacity: {port1d.Capacity}";
                }

                if (port is NodeSystemLib2.FormatValue.InputPortValueDouble) {
                    var port1d = (NodeSystemLib2.FormatValue.InputPortValueDouble)port;
                    additional = $"Value queue size: {port1d.Count}";
                }

                if (port is NodeSystemLib2.FormatValue.OutputPortValueDouble) {
                    var port1d = (NodeSystemLib2.FormatValue.OutputPortValueDouble)port;
                    additional = $"Buffered values: {port1d.BufferedValueCount}";
                }

                toolTipNodeInfo.Show($"Node: {graphNode.Title}\nData Type: {connectorTypeName}\n{additional}", nodeGraphPanel, point);
            }
        }

        private void nodeGraphPanel_Load(object sender, EventArgs e) {

        }

        //private void nodeGraphPanel_OnConnectorMouseLeave(object sender, NodeGraphPanelConnectorMouseLeaveArgs args) {
        //    toolTipNodeInfo.Hide(nodeGraphPanel);
        //}

    }

    /// <summary>
    /// Brücke zwischen Node Fundament und graphischer Darstellung von diesem
    /// </summary>
    public class GraphNode : EC.Node {

        public Node Node { get; }

        public Guid InstanceId { get; }

        public Guid FactoryId { get; private set; }

        public string UniqueName { get; private set; }
        
        private bool NodeOwnerDraw { get; }

        public static GraphNode FromXml(XmlNode node, Graph g) {
            if (node?.Attributes == null) return null;

            var x          = node.TryGetAttribute("x",    otherwise: "0");
            var y          = node.TryGetAttribute("y",    otherwise: "0");
            var id         = node.TryGetAttribute("id",   otherwise: "");
            var uniqueName = node.TryGetAttribute("uniqueName", otherwise: "");
            var factoryId  = new Guid(node.TryGetAttribute("factoryId", otherwise: Guid.Empty.ToString()));

            //XmlNode settings = null;
            //for (int i = 0; i < factorySettings.Count; i++) {
            //    if (Guid.Parse(factorySettings[i].TryGetAttribute("guid", Guid.Empty.ToString())).Equals(factoryId)) {
            //        settings = factorySettings[i];
            //    }

            //}

            var nodeInfo   = node.SelectSingleNode("node");
            if (nodeInfo?.Attributes == null) return null;
            var type = nodeInfo.TryGetAttribute("type", otherwise: "");
            var descr = nodeInfo.TryGetAttribute("description", otherwise: "");

            MetricManager.MetricInfo info = null;
            Node metricNode               = null;
            Exception ex                  = null;

            foreach (var metric in GlobalSettings.Instance.MetricManager.Metrics) {
                if (metric.UniqueName == uniqueName && metric.FactoryGuid == factoryId) {
                    try {
                        metricNode = metric.CreateInstance(g, nodeInfo);
                        metricNode.Description = descr;
                    } catch (Exception e) {
                        ex = e;
                    }
                    break;
                }
            }

            if (metricNode == null) {
                metricNode = new Metrics.MetricUnknownNode(nodeInfo, g);
                metricNode.Description = descr;

                if (ex != null) {
                    GlobalSettings.Instance.UserLog.Add(new NodeMessage(metricNode, LogMessage.LogType.Error, $"Error while creating instance of type {type}. Exception: {ex}"));
                } else {
                    GlobalSettings.Instance.UserLog.Add(new NodeMessage(metricNode, LogMessage.LogType.Error, $"Error while creating instance of type {type}. Device not connected/Plugin missing?"));
                }
            }


            return new GraphNode(metricNode, factoryId, uniqueName, int.Parse(x), int.Parse(y), id);
        }

        public GraphNode(Node node, Guid factoryId, string uniqueName, int pX, int pY, string guid = "") {
            Node = node;
            Title = GetName();
            NodeOwnerDraw = node is NodeSystemLib2.Generic.INodeUi;

            Location = new Point(pX, pY);

            InstanceId = guid == "" ? Guid.NewGuid() : Guid.Parse(guid);
            FactoryId = factoryId;
            UniqueName = uniqueName;

            int InputCount = 0;
            int OutputCount = 0;

            foreach (var port in Node.InputPorts) {
                var connector = new InputGraphConnector(port, this);
                AddPort(connector);
                InputCount++;
            }

            foreach (var port in Node.OutputPorts) {
                var connector = new OutputGraphConnector(port, this);
                AddPort(connector);
                OutputCount++;
            }

            node.PortAdded += Node_PortAdded;
            node.PortRemoved += Node_PortRemoved;

            //UpdateHeight();

            var descAttr = node.GetAttribute("Description");
            if (descAttr != null) descAttr.Changed += DescAttr_Changed;

            if (NodeOwnerDraw) {
                (node as NodeSystemLib2.Generic.INodeUi).OnLoad(this);
            }
        }

        private void DescAttr_Changed(object sender, NodeSystemLib2.Generic.NodeAttributes.AttributeChangedEventArgs e) {
            Title = GetName();
            Parent.InvalidateNode(this);
        }

        private void Node_PortRemoved(object sender, Node.PortChangedEventArgs e) {
            if (e.Port.FlowDirection == Port.Direction.Input) {
                RemovePort(InputPorts.ElementAt(e.Index));
            } else {
                RemovePort(OutputPorts.ElementAt(e.Index));
            }
            Parent.Parent.Invalidate();
        }

        private void Node_PortAdded(object sender, Node.PortChangedEventArgs e) {
            if (e.Port.FlowDirection == Port.Direction.Input) {
                var connector = new InputGraphConnector((InputPort)e.Port, this);
                AddPort(connector);
            }

            if (e.Port.FlowDirection == Port.Direction.Output) {
                var connector = new OutputGraphConnector((OutputPort)e.Port, this);
                AddPort(connector);
            }

        //    GraphConnector connector;

        //    foreach (var con in MConnectors.OfType<GraphConnector>().Where(c => c.Type == ConnectorType.InputConnector)) {
        //        for (int i = 0; i < Node.InputPorts.Count; i++) {
        //            if (Node.InputPorts[i] == con.Port) {
        //                con.Index = i;
        //            }
        //        }
        //    }

        //    foreach (var con in MConnectors.OfType<GraphConnector>().Where(c => c.Type == ConnectorType.OutputConnector)) {
        //        for (int i = 0; i < Node.OutputPorts.Count; i++) {
        //            if (Node.OutputPorts[i] == con.Port) {
        //                con.Index = i;
        //            }
        //        }
        //    }

        //    var index = e.Index;
        //    if (e.Port.FlowDirection == Port.Direction.Input) {
        //        if (index == -1) index = MConnectors.Count(c => c.Type == ConnectorType.InputConnector);
        //        connector = new GraphConnector(e.Port, this, index) { Tag = e.Port };
        //    } else {
        //        if (index == -1) index = MConnectors.Count(c => c.Type == ConnectorType.OutputConnector);
        //        connector = new GraphConnector(e.Port, this, index) { Tag = e.Port };
        //    }

        //    MConnectors.Add(connector);

        //    UpdateHeight();
        //    ParentView.ParentPanel.Invalidate();
        }

        //private void UpdateHeight() {
        //    Height = Math.Max(MConnectors.Count(c => c.Type == ConnectorType.InputConnector), 
        //                      MConnectors.Count(c => c.Type == ConnectorType.OutputConnector)) * 13 + 45;
        //}

        public void Serialize(XmlWriter writer, NodeEditor editor) {
            writer.WriteAttributeString("id", InstanceId.ToString());
            writer.WriteAttributeString("factoryId", FactoryId.ToString());
            writer.WriteAttributeString("uniqueName", UniqueName);
            writer.WriteAttributeString("x", Location.X.ToString());
            writer.WriteAttributeString("y", Location.Y.ToString());
            writer.WriteAttributeString("title", Title);
            
            Node.Serialize(writer);
        }

        public override string ToString() {
            return GetName();
        }

        protected string GetName() {
            if (!string.IsNullOrEmpty(Node.Description) && string.Compare(Node.Description, Node.Name, StringComparison.Ordinal) != 0) {
                return $"{Node.Description} ({Node.Name})";
            }
            return Node.Name;
        }

        //public override void Draw(PaintEventArgs e) {
        //    base.Draw(e);
        //    if (NodeOwnerDraw) {
        //        ((NodeSystemLib2.Generic.INodeUi) Node).OnDraw(ParentView.ParentPanel.ViewToControl(HitRectangle), e.Graphics);
        //    }
        //}

    }

    /// <summary>
    /// Brücke zwischen Node Port und graphischer Darstellung von diesem
    /// </summary>
    public class InputGraphConnector : EC.InputPort {

        public InputPort Port { get; }

        public Guid InstanceId { get; }

        public InputGraphConnector(InputPort port, EC.Node pParent)
            : base(pParent,
                   port.Name,
                   GetDataType(port.DataType)) {

            Port = port;
            Port.ConnectionChanged += Port_ConnectionChanged;
            InstanceId = Guid.NewGuid();
        }

        private void Port_ConnectionChanged(object sender, ConnectionModifiedEventArgs e) {
            if (e.Action == ConnectionModifiedEventArgs.Modifier.Changed && e.Connection == null) {
                Parent.Parent.Disconnect(this);
            }
        }

        private static EC.PortDataType GetDataType(PortDataType type) {
            return GlobalSettings.Instance.NodeDataTypes[type];
        }

    }


    public class OutputGraphConnector : EC.OutputPort {

        public OutputPort Port { get; }

        public Guid InstanceId { get; }

        public OutputGraphConnector(OutputPort port, EC.Node pParent)
            : base(pParent,
                   port.Name,
                   GetDataType(port.DataType)) {

            Port = port;
            Port.ConnectionRemoved += Port_ConnectionRemoved;
            InstanceId = Guid.NewGuid();
        }

        private void Port_ConnectionRemoved(object sender, ConnectionModifiedEventArgs e) {
            var screen = Parent.Parent;
            var port = Ports.OfType<InputGraphConnector>().FirstOrDefault(p => p.Port == e.Connection);
            if (port != null) {
                port.Connection = null;
                this.RemoveConnection(port);
            }
        }

        private static EC.PortDataType GetDataType(PortDataType type) {
            return GlobalSettings.Instance.NodeDataTypes[type];
        }

    }


}
