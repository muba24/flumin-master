using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using OpenTK.Graphics.OpenGL;
using System.IO;
using OpenTK;

namespace NewOpenGLRenderer {
    public partial class Plot : UserControl {

        private GLControl               _glctrl;
        private bool                    _loaded;
        private bool                    _antialias;
        private bool                    _pressed;
        private bool                    _select;
        private Point                   _pressStart;
        private Tuple<double,double>    _pressAxisX;
        private Tuple<double,double>    _pressAxisY;
        private DataSet                 _set;

        public DataSet Set {
            get {
                return _set;
            }
            set {
                _set = value;
                plotAxisHorz.Axis = _set?.AxisX;
                plotAxisVert.Axis = _set?.AxisY;
                Plot_Resize(this, EventArgs.Empty);
                _set?.Update();
            }
        }

        public Color GraphBackColor { get; set; }

        public Padding Border { get; set; }

        public bool Antialias {
            get {
                return _antialias;
            }
            set {
                _antialias = value;
                if (_loaded) {
                    if (_antialias) {
                        GL.Enable(EnableCap.Blend);
                        GL.Enable(EnableCap.AlphaTest);
                        GL.Enable(EnableCap.LineSmooth);
                        GL.Enable(EnableCap.Multisample);
                        GL.Hint(HintTarget.LineSmoothHint, HintMode.Nicest);
                        GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);
                    } else {
                        GL.Disable(EnableCap.Multisample);
                        GL.Disable(EnableCap.LineSmooth);
                        GL.Disable(EnableCap.AlphaTest);
                        GL.Disable(EnableCap.Blend);
                    }
                }
            }
        }

        public event EventHandler GLLoaded;

        public Plot() {
            InitializeComponent();
        }

        private void CreateGLControl() {
            _glctrl                 = new GLControl();

            _glctrl.Width           = this.Width / 2;
            _glctrl.Height          = this.Height / 2;
            _glctrl.Location        = new Point(10, 10);
            _glctrl.Visible         = true;
            _glctrl.VSync           = false;
            _glctrl.MouseDown      += _glctrl_MouseDown;
            _glctrl.MouseUp        += _glctrl_MouseUp;
            _glctrl.MouseMove      += _glctrl_MouseMove;
            _glctrl.MouseWheel     += _glctrl_MouseWheel;

            this.Controls.Add(_glctrl);
        }

        private void _glctrl_MouseWheel(object sender, MouseEventArgs e) {
            float c = e.Delta < 0 ? 1.25f : 1f / 1.25f;

            var zoomAtX = (e.Location.X / (float)_glctrl.Width)  * Set.AxisX.Range + Set.AxisX.VisibleMinimum;
            var zoomAtY = ((_glctrl.Height - e.Location.Y) / (float)_glctrl.Height) * Set.AxisY.Range + Set.AxisY.VisibleMinimum;

            Set.AxisX.VisibleMinimum = (Set.AxisX.VisibleMinimum - zoomAtX) * c + zoomAtX;
            Set.AxisX.VisibleMaximum = (Set.AxisX.VisibleMaximum - zoomAtX) * c + zoomAtX;

            Set.AxisY.VisibleMinimum = (Set.AxisY.VisibleMinimum - zoomAtY) * c + zoomAtY;
            Set.AxisY.VisibleMaximum = (Set.AxisY.VisibleMaximum - zoomAtY) * c + zoomAtY;
            Set.Update();

            InvalidateAll();
        }

        private void _glctrl_MouseMove(object sender, MouseEventArgs e) {
            if (_select) {
                var leftTop = ScreenToCoordinates(_pressStart);
                var rightBottom = ScreenToCoordinates(e.Location);
                Set.Selection = new RectangleF(leftTop, new SizeF(rightBottom.X - leftTop.X, rightBottom.Y - leftTop.Y));
                Set.SelectionVisible = true;
                this.Invalidate();
                return;
            }

            if (_pressed) {
                var deltaX = (e.Location.X - _pressStart.X) / (double)_glctrl.Width * Set.AxisX.Range;
                var deltaY = (e.Location.Y - _pressStart.Y) / (double)_glctrl.Height * Set.AxisY.Range;
                
                if (_pressAxisX.Item1 - deltaX >= Set.AxisX.AbsoluteMinimum && _pressAxisX.Item2 - deltaX <= Set.AxisX.AbsoluteMaximum) {
                    Set.AxisX.VisibleMinimum = _pressAxisX.Item1 - deltaX;
                    Set.AxisX.VisibleMaximum = _pressAxisX.Item2 - deltaX;
                }

                if (_pressAxisY.Item1 + deltaY >= Set.AxisY.AbsoluteMinimum && _pressAxisY.Item2 + deltaY <= Set.AxisY.AbsoluteMaximum) {
                    Set.AxisY.VisibleMinimum = _pressAxisY.Item1 + deltaY;
                    Set.AxisY.VisibleMaximum = _pressAxisY.Item2 + deltaY;
                }

                Set.Update();
                this.Invalidate();
            }
        }

        public PointF ScreenToCoordinates(Point p) {
            var x = Set.AxisX.ScreenToPosition(p.X);
            var y = Set.AxisY.ScreenToPosition(p.Y);
            return new PointF((float)x, (float)y);
        }

        private void InvalidateAll() {
            Invalidate();
            plotAxisHorz.Invalidate();
            plotAxisVert.Invalidate();
        }

        private void _glctrl_MouseUp(object sender, MouseEventArgs e) {
            if (_select) {
                Set.AxisX.VisibleMinimum = Set.Selection.Left;
                Set.AxisX.VisibleMaximum = Set.Selection.Right;
                Set.AxisY.VisibleMinimum = Set.Selection.Bottom;
                Set.AxisY.VisibleMaximum = Set.Selection.Top;
                Set.Update();
            }

            _pressed = false;
            _select = false;
            if (Set != null) Set.SelectionVisible = false;
            tmrAxisRefresh.Enabled = false;
            InvalidateAll();
        }

        private void _glctrl_MouseDown(object sender, MouseEventArgs e) {
            if (e.Button == MouseButtons.Left) {
                if (Set == null) return;

                if (Control.ModifierKeys == Keys.Alt) {
                    _select = true;
                }
                _pressStart = e.Location;
                _pressAxisX = new Tuple<double, double>(Set.AxisX.VisibleMinimum, Set.AxisX.VisibleMaximum);
                _pressAxisY = new Tuple<double, double>(Set.AxisY.VisibleMinimum, Set.AxisY.VisibleMaximum);
                _pressed = true;
                tmrAxisRefresh.Enabled = true;
            }
        }

        private void Plot_Load(object sender, EventArgs e) {
            if (ResolveDesignMode(this)) return;

            CreateGLControl();

            _glctrl.MakeCurrent();
            GL.ClearColor(GraphBackColor);

            if (_antialias) {
                GL.Enable(EnableCap.Blend);
                GL.Enable(EnableCap.AlphaTest);
                GL.Enable(EnableCap.LineSmooth);
                GL.Enable(EnableCap.Multisample);
                GL.Hint(HintTarget.LineSmoothHint, HintMode.Nicest);
                GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);
            } else {
                GL.Disable(EnableCap.Multisample);
                GL.Disable(EnableCap.LineSmooth);
                GL.Disable(EnableCap.AlphaTest);
                GL.Disable(EnableCap.Blend);
            }

            _loaded = true;

            GLLoaded?.Invoke(this, EventArgs.Empty);
            this.OnResize(EventArgs.Empty);
        }

        public void CreateLegend() {
            toolStrip1.Items.Clear();

            toolStrip1.ShowItemToolTips = true;

            foreach (var data in Set.Data) {
                var bmp = new Bitmap(16, 16);
                using (var g = Graphics.FromImage(bmp)) {
                    g.Clear(data.LineColor);
                }

                var tlbutton = new ToolStripButton {
                    Alignment    = ToolStripItemAlignment.Right,
                    Margin       = new Padding(2, 2, data == Set.Data.First() ? Border.Right : 2, 2),
                    Image        = bmp,
                    Checked      = data.Visible,
                    CheckOnClick = true,
                    ToolTipText  = data.Name
                };

                tlbutton.Click += (obj, ev) => {
                    data.Visible = !data.Visible;
                    this.Invalidate();
                };

                tlbutton.MouseEnter += (obj, ev) => {
                    data.Selected = true;
                    this.Invalidate();
                };

                tlbutton.MouseLeave += (obj, ev) => {
                    data.Selected = false;
                    this.Invalidate();
                };

                toolStrip1.Items.Add(tlbutton);
            }

            var buttonFitX = new ToolStripButton {
                Alignment = ToolStripItemAlignment.Left,
                Text = "Fit X Axis"
            };

            buttonFitX.Click += toolStripButtonFitAxisX_Click;
            toolStrip1.Items.Add(buttonFitX);
        }

        private void Plot_Resize(object sender, EventArgs e) {
            if (!_loaded) return;
            if (IsDisposed) return;
            if (!Visible) return;

            _glctrl.Left        = Border.Left;
            _glctrl.Top         = Border.Top + toolStrip1.Height;
            _glctrl.Width       = this.Width - Border.Horizontal;
            _glctrl.Height      = this.Height - Border.Vertical - toolStrip1.Height;

            plotAxisVert.Left    = 0;
            plotAxisVert.Top     = Border.Top + toolStrip1.Height;
            plotAxisVert.Width   = Border.Left;
            plotAxisVert.Height  = this.Height - Border.Vertical - toolStrip1.Height;

            plotAxisHorz.Left    = Border.Left;
            plotAxisHorz.Top     = Border.Top + toolStrip1.Height + _glctrl.Height;
            plotAxisHorz.Width   = _glctrl.Width;
            plotAxisHorz.Height  = Border.Bottom;


            if (Set != null) {
                Set.ParentSize = _glctrl.Size;
                Set.AxisX.Size = plotAxisHorz.Size;
                Set.AxisY.Size = plotAxisVert.Size;
                Set.Update();
                plotAxisVert.Invalidate();
                plotAxisHorz.Invalidate();
            }

            _glctrl.MakeCurrent();
            SetupViewport();
        }

        public void UpdateSizes() {
            Plot_Resize(this, EventArgs.Empty);
        }

        private void Plot_Paint(object sender, PaintEventArgs e) {
            if (!_loaded) return;

            _glctrl.MakeCurrent();

            GL.Clear(ClearBufferMask.ColorBufferBit);
            Set?.Render();
            _glctrl.SwapBuffers();
        }

        private void SetupViewport() {
            GL.MatrixMode(MatrixMode.Projection);
            GL.LoadIdentity();
            GL.Viewport(0, 0, _glctrl.Width, _glctrl.Height);
        }

        private void picAxisVert_Paint(object sender, PaintEventArgs e) {
            if (!_loaded) return;
            e.Graphics.Clear(BackColor);
            Set?.AxisY.Render(e.Graphics);
        }
        
        private void picAxisHorz_Paint(object sender, PaintEventArgs e) {
            if (!_loaded) return;
            e.Graphics.Clear(BackColor);
            Set?.AxisX.Render(e.Graphics);
        }

        private static bool ResolveDesignMode(Control control) {
            var designModeProperty = control.GetType().GetProperty(
                "DesignMode",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic
            );

            var designMode = (bool)designModeProperty.GetValue(control, null);

            if (control.Parent != null) {
                designMode |= ResolveDesignMode(control.Parent);
            }

            return designMode;
        }

        private void tmrAxisRefresh_Tick(object sender, EventArgs e) {
            plotAxisHorz.Refresh();
            plotAxisVert.Refresh();
            this.Invalidate();
            this.Update();
            this.Refresh();
            _glctrl.Invalidate();
            _glctrl.Update();
            _glctrl.Refresh();
        }

        private void toolStripButtonFitAxisX_Click(object sender, EventArgs e) {
            if (Set is TimeDataSet) {
                var timeSet = (TimeDataSet)Set;
                var refTime = timeSet.GetCurrentTimeReference();
                timeSet.AxisX.VisibleMinimum = timeSet.Data.OfType<ITimeData>().Min(d => d.Duration.Begin - refTime).AsSeconds() * timeSet.SamplesPerSecond;
                timeSet.AxisX.VisibleMaximum = timeSet.Data.OfType<ITimeData>().Max(d => d.Duration.End - refTime).AsSeconds() * timeSet.SamplesPerSecond;
                if (timeSet.AxisX.VisibleMaximum <= timeSet.AxisX.VisibleMinimum) {
                    timeSet.AxisX.VisibleMaximum = timeSet.AxisX.VisibleMinimum + timeSet.SamplesPerSecond;
                }
                timeSet.Update();
                InvalidateAll();
            }
        }

        private void plotAxisVert_AxisChanged(object sender, PlotAxis.AxisChangedEventArgs e) {
            Set?.Update();
            if (!tmrAxisRefresh.Enabled) {
                Refresh();
            }
        }

        private void plotAxisHorz_AxisChanged(object sender, PlotAxis.AxisChangedEventArgs e) {
            Set?.Update();
            if (!tmrAxisRefresh.Enabled) {
                Refresh();
            }
        }

        private void Plot_DragEnter(object sender, DragEventArgs e) {
            e.Effect = DragDropEffects.Copy;
        }

        private void Plot_DragDrop(object sender, DragEventArgs e) {
            //OnDragDrop(e);
        }
    }
}
