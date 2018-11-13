using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace NodeEditorLib.EditorControl {

    public class Node : INotifyPropertyChanged {

        private NodeScreen _parent;
        private Point _location;
        private Size _size;
        private string _title;
        private int _zIndex;
        private bool _selected;

        private List<InputPort> _inputs   = new List<InputPort>();
        private List<OutputPort> _outputs = new List<OutputPort>();

        private Color _colorTitle;
        private Brush _brushTitle;

        private Color _colorBackground;
        private Brush _brushBackground;

        private Color _colorBackgroundSelected;
        private Brush _brushBackgroundSelected;

        private Color _colorBorder;
        private Pen _penBorder;

        private Color _colorBorderSelected;
        private Pen _penBorderSelected;

        private Color _colorText;
        private Brush _brushText;

        public Node() {
            ColorTitle = Color.FromArgb(255, 55, 55, 65);
            ColorBackground = Color.FromArgb(255, 30, 30, 40);
            ColorBorder = Color.FromArgb(255, 50, 50, 60);
            ColorText = Color.White;
            UpdateLayout();
        }

        private void ChangeBrush(Color c, ref Brush b) {
            b?.Dispose();
            b = new SolidBrush(c);
        }

        private void ChangePen(Color c, ref Pen b) {
            b?.Dispose();
            b = new Pen(c);
        }

        public void AddPort(InputPort port) {
            var freeIndex = _inputs.Count;
            AddPort(freeIndex, port);
        }

        public void AddPort(OutputPort port) {
            var freeIndex = _outputs.Count;
            AddPort(freeIndex, port);
        }

        public void AddPort(int key, InputPort input) {
            if (_inputs.Contains(input)) throw new InvalidOperationException();
            //if (_inputs.ContainsKey(key)) throw new InvalidOperationException();
            _inputs.Insert(key, input);
            UpdateLayout();
        }

        public void AddPort(int key, OutputPort output) {
            if (_outputs.Contains(output)) throw new InvalidOperationException();
            _outputs.Insert(key, output);
            UpdateLayout();
        }

        public void RemovePort(InputPort port) {
            Parent.Disconnect(port);
            _inputs.Remove(port);
            UpdateLayout();
        }

        public void RemovePort(OutputPort port) {
            for (int i = port.ConnectionCount - 1; i >= 0; i--) {
                Parent.Disconnect(port.Ports[i]);
            }
            _outputs.Remove(port);
            UpdateLayout();
        }

        public void CompactPorts() {
            //var keys = _inputs.Keys.OrderBy(k => k).ToArray();
            //var countDistinct = keys.Distinct().Count();

        }

        #region Properties

        public int TitleHeight => Parent?.Font.Height ?? 0;

        public Color ColorTitle {
            get { return _colorTitle; }
            set {
                _colorTitle = value;
                ChangeBrush(_colorTitle, ref _brushTitle);
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ColorTitle)));
            }
        }

        public Color ColorText {
            get { return _colorText; }
            set {
                _colorText = value;
                ChangeBrush(_colorText, ref _brushText);
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ColorText)));
            }
        }

        public Color ColorBackground {
            get { return _colorBackground; }
            set {
                _colorBackground = value;
                _colorBackgroundSelected = ControlPaint.Light(_colorBorder, 0.55f);
                ChangeBrush(_colorBackground, ref _brushBackground);
                ChangeBrush(_colorBackgroundSelected, ref _brushBackgroundSelected);
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ColorBackground)));
            }
        }

        public Color ColorBorder {
            get { return _colorBorder; }
            set {
                _colorBorder = value;
                _colorBorderSelected = ControlPaint.Light(_colorBorder, 0.55f);
                ChangePen(_colorBorder, ref _penBorder);
                ChangePen(_colorBorderSelected, ref _penBorderSelected);
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ColorBorder)));
            }
        }

        public NodeScreen Parent {
            get {
                return _parent;
            }
            set {
                _parent = value;
                UpdateLayout();
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Parent)));
            }
        }

        public Point Location {
            get { return _location; }
            set {
                _location = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Location)));
                Parent?.InvalidateScreen();
            }
        }

        public int Width => Size.Width;
        public int Height => Size.Height;

        private Size Size {
            get { return _size; }
            set {
                _size = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Size)));
                Parent?.InvalidateScreen();
            }
        }

        public string Title {
            get { return _title; }
            set {
                _title = value;
                UpdateLayout();
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Title)));
            }
        }

        public int ZIndex {
            get { return _zIndex; }
            set {
                if (_zIndex != value) {
                    _zIndex = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ZIndex)));
                }
            }
        }

        public bool Selected {
            get { return _selected; }
            set {
                if (value != _selected) {
                    _selected = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Selected)));
                }
            }
        }

        public IEnumerable<InputPort> InputPorts => _inputs;
        public IEnumerable<OutputPort> OutputPorts => _outputs;

        public Rectangle Area => new Rectangle(Location, Size);

        #endregion

        public event PropertyChangedEventHandler PropertyChanged;

        public Rectangle PortRectangle(Port port) {
            if (port is InputPort) {
                return new Rectangle(Location.X + port.Margin.Left, Location.Y + TitleHeight + port.Top + port.Margin.Top, port.Size.Width, port.Size.Height);
            } else {
                return new Rectangle(Location.X + Size.Width - port.Size.Width - port.Margin.Horizontal - 1, Location.Y + TitleHeight + port.Top + port.Margin.Top, port.Size.Width, port.Size.Height);
            }
        }

        public Port PortFromWorldPoint(Point p) {
            foreach (var port in _inputs.OfType<Port>().Concat(_outputs)) {
                if (PortRectangle(port).Contains(p)) {
                    return port;
                }
            }
            return null;
        }

        public void UpdateLayout() {
            var inputs = _inputs;
            var outputs = _outputs;

            var dyIn = 0;
            foreach (var port in inputs) {
                port.Top = dyIn;
                dyIn += port.Margin.Vertical + port.Size.Height;
            }

            var dyOut = 0;
            foreach (var port in outputs) {
                port.Top = dyOut;
                dyOut += port.Margin.Vertical + port.Size.Height;
            }

            int width = 100;
            if (!string.IsNullOrEmpty(Title) && Parent != null) {
                var rcText = TextRenderer.MeasureText(Title, Parent.Font);
                width = Math.Max(width, rcText.Width + 10);
            }

            Size = new Size(width, TitleHeight + Math.Max(dyIn, dyOut));
            Parent?.InvalidateScreen();
        }

        public void Render(Graphics g) {
            var brushBg = Selected ? _brushBackgroundSelected : _brushBackground;
            var penBorder = Selected ? _penBorderSelected : _penBorder;

            var grBrush = new LinearGradientBrush(
                new Point(0, Area.Top), 
                new Point(0, Area.Top + TitleHeight + 1), 
                ColorTitle, 
                Selected ? _colorBackgroundSelected : _colorBackground
            );

            g.FillRectangle(brushBg, Area.X, Area.Y, Area.Width, Area.Height);
            g.FillRectangle(Brushes.Black, Area.X + 1, Area.Bottom, Area.Width - 1, 2);
            g.FillRectangle(grBrush, Area.X, Area.Y, Area.Width, TitleHeight);
            g.DrawRectangle(penBorder, Area.X, Area.Y, Area.Width, Area.Height);
            g.DrawString(Title, Parent.Font, _brushText, Location);

            grBrush.Dispose();

            foreach (var port in _inputs) {
                var c = g.BeginContainer();
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                g.TranslateTransform(Location.X + port.Margin.Left, TitleHeight + Location.Y + port.Top);
                port.Render(g);
                g.EndContainer(c);
            }

            foreach (var port in _outputs) {
                var c = g.BeginContainer();
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                g.TranslateTransform(Location.X + Size.Width - port.Size.Width - port.Margin.Horizontal - 1, TitleHeight + Location.Y + port.Top);
                port.Render(g);
                g.EndContainer(c);
            }
        }
    }

}
