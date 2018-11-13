using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NodeEditorLib.EditorControl {
    public class NodeScreen {

        private ObservableCollection<Node> _nodes = new ObservableCollection<Node>();

        public NodeEditorControl Parent { get; }
        public Collection<Node> Nodes => _nodes;
        public Point Center { get; set; } = Point.Empty;
        public float Zoom { get; set; } = 1f;
        public Font Font { get; set; } = SystemFonts.DefaultFont;

        public event EventHandler<NodeCollectionModifiedEventArgs> NodeCollectionModified;
        public event EventHandler<SelectionChangedEventArgs> SelectionChanged;
        public event EventHandler<LinkEventArgs> LinkCreated;
        public event EventHandler<LinkEventArgs> LinkDestroyed;

        public NodeScreen(NodeEditorControl parent) {
            Parent = parent;
            _nodes.CollectionChanged += _nodes_CollectionChanged;
        }

        public Rectangle GetView() {
            var width  = (int)(Parent.Width / Zoom);
            var height = (int)(Parent.Height / Zoom);
            return new Rectangle(Center.X - width / 2, Center.Y - height / 2, width, height);
        }

        public Point ControlPointToWorldPoint(Point p) {
            return new Point((int)(p.X / Zoom + Center.X - Parent.Width / (2 * Zoom)), 
                             (int)(p.Y / Zoom + Center.Y - Parent.Height / (2 * Zoom)));
        }

        public Point WorldPointToControlPoint(Point p) {
            return new Point((int)((p.X - Center.X + Parent.Width / (2 * Zoom)) * Zoom),
                            (int)((p.Y - Center.Y + Parent.Height / (2 * Zoom)) * Zoom));
        }

        public RectangleF WorldRectToControlRect(Rectangle rc) {
            var p1 = WorldPointToControlPoint(rc.Location);
            var p2 = WorldPointToControlPoint(new Point(rc.Right, rc.Bottom));
            return new RectangleF(p1.X, p1.Y, p2.X - p1.X, p2.Y - p1.Y);
        }

        public RectangleF ControlRectToWorldRect(Rectangle rc) {
            var p1 = ControlPointToWorldPoint(rc.Location);
            var p2 = ControlPointToWorldPoint(new Point(rc.Right, rc.Bottom));
            return new RectangleF(p1.X, p1.Y, p2.X - p1.X, p2.Y - p1.Y);
        }

        public void Connect(InputPort pIn, OutputPort pOut) {
            if (pIn.Connection != null) Disconnect(pIn);
            pIn.Connection = pOut;
            pOut.AddConnection(pIn);
            LinkCreated?.Invoke(this, new LinkEventArgs(pIn, pOut));
        }

        public void Disconnect(InputPort pIn) {
            var pOut = pIn.Connection;
            if (pOut != null) {
                pOut.RemoveConnection(pIn);
                pIn.Connection = null;
                LinkDestroyed?.Invoke(this, new LinkEventArgs(pIn, pOut));
            }
        }

        public void SelectNodes(Node selNode) {
            var selChanged = false;

            foreach (var node in Nodes) {
                if (node.Selected) {
                    node.Selected = false;
                    selChanged = true;
                }
            }

            if (selNode != null) {
                selNode.Selected = true;
                selNode.ZIndex = Nodes.Max(n => n.ZIndex) + 1;
                selChanged = true;
            }

            if (selChanged) {
                SelectionChanged?.Invoke(this, new SelectionChangedEventArgs());
            }
        }

        public void SelectNodes(IEnumerable<Node> selNodes) {
            var selChanged = false;

            foreach (var node in selNodes) {
                if (!node.Selected) {
                    node.Selected = true;
                    selChanged = true;
                }
            }

            foreach (var node in Nodes.Except(selNodes)) {
                if (node.Selected) {
                    node.Selected = false;
                    selChanged = true;
                }
            }

            if (selChanged) {
                SelectionChanged?.Invoke(this, new SelectionChangedEventArgs());
            }
        }

        private void _nodes_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e) {
            if (e.OldItems != null) {
                foreach (Node oldItem in e.OldItems) {
                    NodeCollectionModified?.Invoke(this, new NodeCollectionModifiedEventArgs {
                        Action = NodeCollectionModifiedEventArgs.ActionType.Removed,
                        Item = oldItem
                    });

                    oldItem.PropertyChanged -= NewItem_PropertyChanged;
                    oldItem.Parent = null;
                }
            }

            if (e.NewItems != null) {
                foreach (Node newItem in e.NewItems) {
                    newItem.Parent = this;
                    newItem.PropertyChanged += NewItem_PropertyChanged;
                    NodeCollectionModified?.Invoke(this, new NodeCollectionModifiedEventArgs {
                        Action = NodeCollectionModifiedEventArgs.ActionType.Added,
                        Item = newItem
                    });
                }
            }

            if (e.NewItems != null || e.OldItems != null) {
                Parent.Invalidate();
            }
        }

        public void InvalidateScreen() {
            Parent.Invalidate();
        }

        public void InvalidateNode(Node n) {
            var rc = WorldRectToControlRect(n.Area).ToRectangle();
            rc.Inflate(3, 3);
            Parent.Invalidate(rc);
        }

        private void NewItem_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e) {
            InvalidateNode((Node)sender);
        }

        public void Render(Graphics g) {
            var view = GetView();

            g.ScaleTransform(Zoom, Zoom);
            g.TranslateTransform(-view.Left, -view.Top);

            foreach (var node in Nodes.OrderBy(n => n.ZIndex)) {
                node.Render(g);
            }

            foreach (var node in Nodes) {
                foreach (var port in node.OutputPorts) {
                    var p1 = node.PortRectangle(port);
                    foreach (var dst in port.Ports) {
                        var p2 = dst.Parent.PortRectangle(dst);
                        var c1 = p1.Center();
                        var c2 = p2.Center();

                        g.DrawBezier(dst.DataType.ColorPen, 
                            c1, 
                            new Point(c1.X + (c2.X - c1.X) / 3, c1.Y), 
                            new Point(c2.X - (c2.X - c1.X) / 3, c2.Y), 
                            p2.Center()
                        );
                    }
                }
            }
        }
    }
}

