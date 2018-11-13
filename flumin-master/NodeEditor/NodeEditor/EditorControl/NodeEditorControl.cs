using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Collections.ObjectModel;

namespace NodeEditorLib.EditorControl {
    public partial class NodeEditorControl : UserControl {

        private enum DragMode {
            DragNone,
            DragNode,
            DragConnection,
            DragScreen,
            DragSelection
        }

        private readonly SelectionRect _selRect = new SelectionRect();
        private DragMode _dragMode;
        private NodeScreen _screen;
        private Port _dragPort;
        private Port _targetPort;
        private Point _dragElementStartPosition;
        private Point _lastDragPosition;

        public event EventHandler<NodeCollectionModifiedEventArgs> NodeCollectionModified;
        public event EventHandler<SelectionChangedEventArgs> SelectionChanged;
        public event EventHandler<LinkEventArgs> LinkCreated;
        public event EventHandler<LinkEventArgs> LinkDestroyed;

        #region Properties

        [ReadOnly(true)]
        public Point Center {
            get { return _screen.Center; }
            set { _screen.Center = value; }
        }

        public float Zoom {
            get { return _screen.Zoom; }
            set { _screen.Zoom = value; }
        }

        public Collection<Node> Nodes => _screen.Nodes;

        public IEnumerable<Node> SelectedNodes => _screen.Nodes.Where(n => n.Selected);

        #endregion

        public NodeEditorControl() {
            InitializeComponent();

            MouseWheel += NodeEditorControl_MouseWheel;
            FontChanged += NodeEditorControl_FontChanged;

            _screen = new NodeScreen(this);
            _screen.NodeCollectionModified += _screen_NodeCollectionModified;
            _screen.SelectionChanged += _screen_SelectionChanged;
            _screen.LinkDestroyed += _screen_LinkDestroyed;
            _screen.LinkCreated += _screen_LinkCreated;
            _screen.Font = this.Font;

            DoubleBuffered = true;
            Center = new Point(this.Width / 2, this.Height / 2);
        }

        private void _screen_LinkDestroyed(object sender, LinkEventArgs e) {
            LinkDestroyed?.Invoke(this, e);
        }

        private void _screen_LinkCreated(object sender, LinkEventArgs e) {
            LinkCreated?.Invoke(this, e);
        }

        private void _screen_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            SelectionChanged?.Invoke(this, e);
        }

        private void NodeEditorControl_FontChanged(object sender, EventArgs e) {
            _screen.Font = this.Font;
        }

        public Point ControlToWorld(Point p) {
            return _screen.ControlPointToWorldPoint(p);
        }

        public Node NodeFromPoint(Point p) {
            return GetTopNodeFromWorldPoint(_screen.ControlPointToWorldPoint(p));
        }

        public void SelectNode(Node n) {
            _screen.SelectNodes(n);
        }

        private IEnumerable<Node> NodesFromWorldPoint(Point p) {
            foreach (var node in Nodes) {
                if (node.Area.Contains(p)) {
                    yield return node;
                }
            }
        }

        private Node GetTopNodeFromWorldPoint(Point p) {
            return NodesFromWorldPoint(p).OrderByDescending(n => n.ZIndex).FirstOrDefault();
        }

        private void ClearConnections(Port p) {
            if (p is OutputPort) {
                var ins = ((OutputPort)p).Ports.ToArray();
                foreach (var pIn in ins) {
                    _screen.Disconnect(pIn);
                }
                System.Diagnostics.Debug.Assert(((OutputPort)p).ConnectionCount == 0);
            } else {
                var pIn = (InputPort)p;
                if (pIn.Connection != null) {
                    var pOut = pIn.Connection;
                    _screen.Disconnect(pIn);
                }
            }
        }

        public void Disconnect(Port source, Port target) {
            if (target is InputPort) {
                _screen.Disconnect((InputPort)target);
            } else {
                _screen.Disconnect((InputPort)source);
            }
        }

        public void CreateConnection(Port source, Port target) {
            if (target is InputPort) {
                _screen.Connect((InputPort)target, (OutputPort)source);
            } else {
                _screen.Connect((InputPort)source, (OutputPort)target);
            }
        }

        #region Event Handlers

        private void _screen_NodeCollectionModified(object sender, NodeCollectionModifiedEventArgs e) {
            if (e.Action == NodeCollectionModifiedEventArgs.ActionType.Removed) {
                foreach (var port in e.Item.InputPorts.ToArray()) {
                    ClearConnections(port);
                }
                foreach (var port in e.Item.OutputPorts.ToArray()) {
                    ClearConnections(port);
                }
            }
            NodeCollectionModified?.Invoke(this, e);
        }

        private void NodeEditorControl_Resize(object sender, EventArgs e) {
            Invalidate();
        }

        private void NodeEditorControl_Paint(object sender, PaintEventArgs e) {
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            e.Graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.High;

            var c = e.Graphics.BeginContainer();
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            e.Graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.High;
            _screen.Render(e.Graphics);
            e.Graphics.EndContainer(c);
            
            if (_dragMode == DragMode.DragConnection) {
                var mouseLocation = PointToClient(Cursor.Position);
                e.Graphics.DrawLine(_targetPort != null ? Pens.LightGreen : Pens.Orange, _dragElementStartPosition, mouseLocation);
            } else if (_dragMode == DragMode.DragSelection) {
                var mousePosition = PointToClient(Cursor.Position);
                _selRect.Update(mousePosition);
                _selRect.Paint(e.Graphics);
            }
        }

        private void NodeEditorControl_MouseDown(object sender, MouseEventArgs e) {
            _lastDragPosition = e.Location;

            var worldPoint = _screen.ControlPointToWorldPoint(e.Location);
            var dragNode = GetTopNodeFromWorldPoint(worldPoint);
            
            if (dragNode != null) {
                _dragPort = dragNode.PortFromWorldPoint(worldPoint);
                if (_dragPort != null) {
                    if (Control.ModifierKeys == Keys.Alt) {
                        ClearConnections(_dragPort);
                        _dragMode = DragMode.DragNone;
                        Invalidate();
                    } else {
                        _dragElementStartPosition = _screen.WorldPointToControlPoint(dragNode.PortRectangle(_dragPort).Center());
                        _dragMode = DragMode.DragConnection;
                    }
                } else {
                    if (!dragNode.Selected) {
                        _screen.SelectNodes(dragNode);
                    }

                    _dragMode = DragMode.DragNode;
                }
            } else {
                if (e.Button == MouseButtons.Left) {
                    _selRect.Start(e.Location);
                    _screen.SelectNodes((Node)null);
                    _dragMode = DragMode.DragSelection;
                } else {
                    _dragElementStartPosition = _screen.Center;
                    _screen.SelectNodes((Node)null);
                    _dragMode = DragMode.DragScreen;
                }
            }
        }

        private void NodeEditorControl_MouseMove(object sender, MouseEventArgs e) {
            switch (_dragMode) {
                case DragMode.DragNode: {
                        var currentInWorld = _screen.ControlPointToWorldPoint(e.Location);
                        var oldInWorld = _screen.ControlPointToWorldPoint(_lastDragPosition);

                        var dx = currentInWorld.X - oldInWorld.X;
                        var dy = currentInWorld.Y - oldInWorld.Y;
                        _lastDragPosition = e.Location;

                        foreach (var node in _screen.Nodes.Where(n => n.Selected)) {
                            node.Location = new Point(
                                node.Location.X + dx,
                                node.Location.Y + dy
                            );
                        }
                    }
                    break;

                case DragMode.DragScreen:
                    if (e.Button == MouseButtons.Middle) {
                        var currentInWorld = _screen.ControlPointToWorldPoint(e.Location);
                        var oldInWorld = _screen.ControlPointToWorldPoint(_lastDragPosition);

                        var dx = currentInWorld.X - oldInWorld.X;
                        var dy = currentInWorld.Y - oldInWorld.Y;
                        _lastDragPosition = e.Location;

                        _screen.Center = new Point(
                            _screen.Center.X - dx, 
                            _screen.Center.Y - dy
                        );
                        Invalidate();
                    }
                    break;

                case DragMode.DragSelection:
                    var area = _screen.ControlRectToWorldRect(_selRect.Area);
                    _screen.SelectNodes(Nodes.Where(n => area.IntersectsWith(n.Area)));
                    Invalidate();
                    break;

                case DragMode.DragConnection:
                    _targetPort = null;

                    var worldPoint = _screen.ControlPointToWorldPoint(e.Location);
                    var targetNode = GetTopNodeFromWorldPoint(worldPoint);

                    if (targetNode != null) {
                        var targetPort = targetNode.PortFromWorldPoint(worldPoint);
                        if (targetPort != null &&
                            targetPort.DataType.Equals(_dragPort.DataType) &&
                            targetPort.FlowDirection != _dragPort.FlowDirection) {

                            _targetPort = targetPort;
                        }
                    }

                    Invalidate();
                    break;

            }
        }

        private void NodeEditorControl_MouseUp(object sender, MouseEventArgs e) {
            switch (_dragMode) {
                case DragMode.DragConnection:
                    if (_targetPort != null && 
                        _targetPort != _dragPort && 
                        _targetPort.FlowDirection != _dragPort.FlowDirection &&
                        _targetPort.Parent != _dragPort.Parent &&
                        _targetPort.DataType.Equals(_dragPort.DataType)) {
                        CreateConnection(_dragPort, _targetPort);
                    }
                    Invalidate();
                    break;

                case DragMode.DragSelection:
                    Invalidate();
                    break;

            }

            _dragMode = DragMode.DragNone;
        }

        private void NodeEditorControl_MouseWheel(object sender, MouseEventArgs e) {
            var worldZoomTo = _screen.ControlPointToWorldPoint(e.Location);
            var deltaX = _screen.Center.X - worldZoomTo.X;
            var deltaY = _screen.Center.Y - worldZoomTo.Y;
            var newZoom = _screen.Zoom  * (1 + e.Delta / 1200.0f);

            var pw = worldZoomTo;
            var pr = e.Location;
            var c = new Point((int)(pw.X + Width / (2 * newZoom) - pr.X / newZoom),
                              (int)(pw.Y + Height / (2 * newZoom) - pr.Y / newZoom));
            
            _screen.Center = c;
            _screen.Zoom = newZoom;

            Invalidate();
        }

        #endregion

    }
}
