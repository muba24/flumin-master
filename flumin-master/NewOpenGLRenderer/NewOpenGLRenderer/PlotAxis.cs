using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace NewOpenGLRenderer {
    public partial class PlotAxis : UserControl {

        /// <summary>
        /// Represents an immutable closed interval. Basically it's a named tuple.
        /// </summary>
        /// <typeparam name="T">Type of interval boundaries</typeparam>
        private class Interval : Tuple<double, double> {
            public Interval(double left, double right) : base(left, right) { }
            public double Left  => Item1;
            public double Right => Item2;
            public double Range => Right - Left;
        }

        /// <summary>
        /// Used to store the initial axis interval for when a boundary resize action starts
        /// </summary>
        private Interval _oldAxisInterval;

        /// <summary>
        /// Mode in which the mouse cursor is.
        /// Used as a statemachine for resizing the axis boundaries.
        /// </summary>
        private enum MouseMode {
            None,
            OverLowerRegion,
            OverUpperRegion,
            DragLower,
            DragUpper
        }

        private MouseMode _mouseMode = MouseMode.None;

        /// <summary>
        /// The area in wich the mouse can trigger a lower bound resize of the axis
        /// </summary>
        private Rectangle _regionLower;

        /// <summary>
        /// The area in which the mouse ca trigger an upper bound resize of the axis
        /// </summary>
        private Rectangle _regionUpper;

        /// <summary>
        /// Point where the mouseDown event ocurred
        /// </summary>
        private Point _dragStart;

        /// <summary>
        /// Gets/sets the axis to be used for display
        /// </summary>
        public Axis Axis {
            get { return _axis; }
            set { _axis = value; UpdateMouseHitRegions(); }
        }

        private Axis _axis;


        public class AxisChangedEventArgs : EventArgs {
            public double Minimum;
            public double Maximum;
        }

        public event EventHandler<AxisChangedEventArgs> AxisChanged;


        public PlotAxis() {
            InitializeComponent();
        }

        private void UpdateMouseHitRegions() {
            if (Axis == null) return;

            _regionUpper = this.ClientRectangle;
            _regionLower = this.ClientRectangle;

            if (Axis.Orientation == Axis.AxisOrientation.Vertical) {
                _regionUpper.Height = Height / 3;
                _regionLower.Height = Height / 3;
                _regionLower.Location = new Point(0, 2 * Height / 3);

            } else if (Axis.Orientation == Axis.AxisOrientation.Horizontal) {
                _regionUpper.Width = Width / 3;
                _regionLower.Width = Width / 3;
                _regionUpper.Location = new Point(2 * Width / 3, 0);
            }
        }

        private void PlotAxis_Paint(object sender, PaintEventArgs e) {
            if (Axis == null) return;

            switch (_mouseMode) {
                case MouseMode.DragLower:
                case MouseMode.OverLowerRegion:
                    e.Graphics.FillRectangle(Brushes.Gray, _regionLower);
                    break;
                case MouseMode.DragUpper:
                case MouseMode.OverUpperRegion:
                    e.Graphics.FillRectangle(Brushes.Gray, _regionUpper);
                    break;
            }

            Axis?.Render(e.Graphics);
        }

        private void PlotAxis_MouseDown(object sender, MouseEventArgs e) {
            if (e.Button != MouseButtons.Left) return;
            if (Axis == null) return;

            if (_mouseMode == MouseMode.OverLowerRegion || _mouseMode == MouseMode.OverUpperRegion) {
                _oldAxisInterval = new Interval(Axis.VisibleMinimum, Axis.VisibleMaximum);
                _dragStart = e.Location;
                timerAxisRefresh.Enabled = true;

                if (_mouseMode == MouseMode.OverLowerRegion) {
                    _mouseMode = MouseMode.DragLower;
                } else {
                    _mouseMode = MouseMode.DragUpper;
                }
            }
        }

        private void PlotAxis_MouseMove(object sender, MouseEventArgs e) {
            if (Axis == null) return;

            switch (_mouseMode) {
                case MouseMode.None:
                    if (_regionLower.Contains(e.Location)) {
                        _mouseMode = MouseMode.OverLowerRegion;
                        Invalidate();
                    } else if (_regionUpper.Contains(e.Location)) {
                        _mouseMode = MouseMode.OverUpperRegion;
                        Invalidate();
                    }
                    break;
                case MouseMode.OverLowerRegion:
                case MouseMode.OverUpperRegion:
                    if (!_regionLower.Contains(e.Location) && !_regionUpper.Contains(e.Location)) {
                        _mouseMode = MouseMode.None;
                        Invalidate();
                    }
                    break;
                case MouseMode.DragLower:
                case MouseMode.DragUpper:
                    {
                        double delta = 0.0;
                        if (Axis.Orientation == Axis.AxisOrientation.Horizontal) {
                            delta = (e.Location.X- _dragStart.X) / Axis.Length * _oldAxisInterval.Range;
                        } else {
                            delta = -(e.Location.Y - _dragStart.Y) / Axis.Length * _oldAxisInterval.Range;
                        }
                        if (_mouseMode == MouseMode.DragLower) Axis.VisibleMinimum = _oldAxisInterval.Left - delta;
                        if (_mouseMode == MouseMode.DragUpper) Axis.VisibleMaximum = _oldAxisInterval.Right - delta;

                        AxisChanged?.Invoke(this, new AxisChangedEventArgs { Minimum = Axis.VisibleMinimum, Maximum = Axis.VisibleMaximum });
                    }

                    break;
            }
        }

        private void PlotAxis_MouseLeave(object sender, EventArgs e) {
            if (Axis == null) return;

            switch (_mouseMode) {
                case MouseMode.OverLowerRegion:
                case MouseMode.OverUpperRegion:
                    _mouseMode = MouseMode.None;
                    Invalidate();
                    break;
            }
        }

        private void PlotAxis_MouseUp(object sender, MouseEventArgs e) {
            if (Axis == null) return;

            if (!ClientRectangle.Contains(e.Location)) {
                _mouseMode = MouseMode.None;
                Invalidate();
                return;
            }

            switch (_mouseMode) {
                case MouseMode.DragLower:
                    _mouseMode = MouseMode.OverLowerRegion;
                    break;
                case MouseMode.DragUpper:
                    _mouseMode = MouseMode.OverUpperRegion;
                    break;
            }
        }

        private void timerAxisRefresh_Tick(object sender, EventArgs e) {
            if (_mouseMode != MouseMode.DragLower && _mouseMode != MouseMode.DragUpper) {
                timerAxisRefresh.Enabled = false;
            }
            Refresh();
        }

        private void PlotAxis_Resize(object sender, EventArgs e) {
            UpdateMouseHitRegions();
        }
    }
}
