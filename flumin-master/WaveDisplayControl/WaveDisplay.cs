using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace WaveDisplayControl {

    public partial class WaveDisplay : UserControl {

        private const int SpaceLeft    = 30;
        private const int SpaceBottom  = 30;
        private const int ScrollSize   = 7;

        public enum Mode {
            Idle,
            Scroll,
            Select
        }

        public double Ymax { get; set; }
        public double Ymin { get; set; }
        private double Ydelta => Ymax - Ymin;

        public Mode CurrentMode { get; private set; }
        public long SampleOffset { get; set; }
        public long SelectionStart { get; set; }
        public long SelectionLength { get; set; }

        public bool CanChangeYAxis { get; set; }

        private int _scrollbarDragLeft = -1;
        private int _pinchFactor = 1;

        private readonly Brush _scrollBarBrushScroll;
        private readonly Brush _scrollBarBrush;

        public int PinchFactor {
            get {
                return _pinchFactor;
            }
            set {
                _pinchFactor = Math.Max(1, value);
                OnZoomChanged?.Invoke(this);
            }
        }

        private long GetPinchedIndex(long index) => index / PinchFactor;

        private long GetUnpinchedOffset(long pinchedIndex) => pinchedIndex * PinchFactor;

        private long PinchedSampleOffset {
            get {
                return GetPinchedIndex(SampleOffset);
            }
            set {
                SampleOffset = GetUnpinchedOffset(value);
            }
        }

        private long PinchedDataLength {
            get {
                if (Renderer != null)
                    return Renderer.DataLength / PinchFactor;
                else
                    return 0;
            }
        }

        public delegate void SelectionChangedHandler(WaveDisplay sender);
        public event SelectionChangedHandler OnSelectionChanged;

        public delegate void ZoomChangedHandler(WaveDisplay sender);
        public event ZoomChangedHandler OnZoomChanged;

        public delegate void ScrollHandler(WaveDisplay sender);
        public new event ScrollHandler OnScroll;

        private IDataRenderer _renderer;
        public IDataRenderer Renderer {
            get {
                return _renderer;
            }
            set {
                if (value != null) {
                    var sigArea = GetSignalArea();
                    value.Resize(sigArea.Width, sigArea.Height);
                }

                _renderer = value;
            }
        }

        public WaveDisplay() {
            InitializeComponent();
            MouseWheel += WaveDisplay_MouseWheel;

            Ymax = 1.5;
            Ymin = -1.5;
            CurrentMode = Mode.Idle;

            _scrollBarBrush = new SolidBrush(Color.Gray);
            _scrollBarBrushScroll = new SolidBrush(Color.FromArgb(120, Color.LightGray));
        }

        private int MapYToRect(double y, Rectangle r) {
            return r.Top + (int)(r.Height - (y - Ymin) / Ydelta * r.Height);
        }

        public Rectangle GetSignalArea() {
            return new Rectangle(SpaceLeft + Padding.Left,
                                 Padding.Top,
                                 ClientSize.Width - Padding.Horizontal - SpaceLeft,
                                 ClientSize.Height - Padding.Vertical - SpaceBottom);
        }

        private Rectangle GetScrollbarArea(Rectangle signalArea) {
            double percentVisible = signalArea.Width / (double)PinchedDataLength;

            int scrollbarWidth = (int)(signalArea.Width * percentVisible);
            if (scrollbarWidth < 40) scrollbarWidth = 40;

            int scrollbarLeft = (int)MapToInterval(
                PinchedSampleOffset,
                new Interval(0, PinchedDataLength - signalArea.Width),
                new Interval(0, signalArea.Width - scrollbarWidth)
            );

            if (scrollbarLeft < 0) {
                scrollbarLeft = 0;
            }

            return new Rectangle(scrollbarLeft + signalArea.X,
                                 signalArea.Top + signalArea.Height - ScrollSize,
                                 scrollbarWidth,
                                 ScrollSize);
        }

        private void Render(Graphics g) {
            g.SmoothingMode = SmoothingMode.None;
            g.CompositingQuality = CompositingQuality.HighSpeed;

            g.Clear(Color.White);

            var sigArea = GetSignalArea();

            RenderYScale(g, sigArea);
            RenderTimeScale(g, sigArea);

            Renderer?.Render(g, sigArea, SampleOffset, sigArea.Width, PinchFactor);

            RenderSelection(g, sigArea);
            g.DrawRectangle(Pens.Black, sigArea);

            if (!IsAllDataVisible) {
                RenderScrollbar(g, sigArea);
            }
        }

        private void RenderSelection(Graphics g, Rectangle sigArea) {
            var scaledSelStart = SelectionStart / PinchFactor;
            var scaledSelLen = SelectionLength / PinchFactor;

            if ((PinchedSampleOffset + sigArea.Width) > scaledSelStart &&
                (scaledSelStart + scaledSelLen) > PinchedSampleOffset) {

                var pixelStart = scaledSelStart - PinchedSampleOffset + sigArea.X;
                var pixelEnd = pixelStart + scaledSelLen;

                if (pixelStart < sigArea.X)
                    pixelStart = sigArea.X;
                if (pixelEnd > sigArea.X + sigArea.Width)
                    pixelEnd = sigArea.X + sigArea.Width;

                using (var fill = new SolidBrush(Color.FromArgb(120, Color.LightBlue))) {
                    g.FillRectangle(fill, new Rectangle(
                        (int)(pixelStart),
                        sigArea.Top,
                        (int)(pixelEnd - pixelStart),
                        sigArea.Height
                    ));
                }

            }
        }

        private void RenderScrollbar(Graphics g, Rectangle sigArea) {
            var scrollArea = GetScrollbarArea(sigArea);
            g.FillRectangle(CurrentMode == Mode.Scroll ? _scrollBarBrushScroll : _scrollBarBrush, scrollArea);
        }

        private void RenderYScale(Graphics g, Rectangle sigArea) {
            var area = new Rectangle(new Point(0, 0), new Size(sigArea.Left, sigArea.Height));
            _renderer?.YScale?.Render(g, area);
        }


        // TODO: X Scale Renderer einführen in die einzelnen Renderer
        private void RenderTimeScale(Graphics g, Rectangle sigArea) {
            for (long x = 0; x < sigArea.Width; x++) {
                if ((x + PinchedSampleOffset) % 100 == 0) {
                    g.DrawLine(
                        Pens.Black,
                        new Point((int)(sigArea.X + x), sigArea.Bottom + 0),
                        new Point((int)(sigArea.X + x), sigArea.Bottom + 3)
                    );

                    g.DrawString(
                        SampleIndexToTimeString(x + PinchedSampleOffset),
                        SystemFonts.CaptionFont,
                        SystemBrushes.ControlText,
                        new PointF(x + sigArea.X, sigArea.Bottom + 5)
                    );
                }
            }
        }

        public bool IsAllDataVisible =>  GetSignalArea().Width >= PinchedDataLength;

        void WaveDisplay_MouseWheel(object sender, MouseEventArgs e) {
            if (e.Delta > 0) {
                PinchFactor += 1000;
            } else if (e.Delta < 0) {
                if (PinchFactor > 1) {
                    PinchFactor -= 1000;
                }
            }
            Invalidate();
        }
        
        private void WaveDisplay_Resize(object sender, EventArgs e) {
            var sigArea = GetSignalArea();
            if (IsAllDataVisible) {
                PinchedSampleOffset = 0;
            } else {
                LimitSampleOffset(sigArea);
            }

            Renderer?.Resize(sigArea.Width, sigArea.Height);

            Invalidate();
        }

        private void WaveDisplay_MouseMove(object sender, MouseEventArgs e) {
            switch (CurrentMode) {
                case Mode.Idle:
                    break;

                case Mode.Scroll:
                    ScrollToMouseLocation(e.Location);
                    Invalidate();
                    break;

                case Mode.Select:
                    SelectToMouseLocation(e.Location);
                    Invalidate();
                    OnSelectionChanged?.Invoke(this);
                    break;

            }
        }

        private void SelectToMouseLocation(Point location) {
            if (Renderer != null) {
                var sigArea = GetSignalArea();
                var selectionEnd = SampleOffset + PinchFactor * (location.X - sigArea.Left);
                SelectionLength = Math.Max(0, selectionEnd - SelectionStart);
                SelectionLength = Math.Min(SelectionLength, Renderer.DataLength - SelectionStart);
            }
        }

        private void ScrollToMouseLocation(Point location) {
            var sigArea = GetSignalArea();
            var scrollArea = GetScrollbarArea(sigArea);
            var scrollLeftOffset = location.X - (_scrollbarDragLeft + sigArea.X);

            PinchedSampleOffset = (long)MapToInterval(
                scrollLeftOffset,
                new Interval(0, sigArea.Width - scrollArea.Width),
                new Interval(0, PinchedDataLength - sigArea.Width)
            );

            LimitSampleOffset(sigArea);

            OnScroll?.Invoke(this);
        }

        private void WaveDisplay_MouseDown(object sender, MouseEventArgs e) {
            var sigArea = GetSignalArea();
            var scrollArea = GetScrollbarArea(sigArea);

            if (e.Button != MouseButtons.Left) return;

            if (CanChangeYAxis && new Rectangle(0, 0, sigArea.Left, sigArea.Height).Contains(e.Location)) {
                var result = new ChangeZoom().ShowDialog(this, Renderer.YScale.YMin, Renderer.YScale.YMax);
                if (result != null) {
                    Renderer.YScale.ValidateAndSetYMax(result.YMax);
                    Renderer.YScale.ValidateAndSetYMin(result.YMin);
                }

            } else if (scrollArea.Contains(e.Location)) {
                _scrollbarDragLeft = e.Location.X - scrollArea.X;
                CurrentMode = Mode.Scroll;

            } else {
                SelectionStart = Math.Max(0, SampleOffset + PinchFactor * (e.Location.X - sigArea.Left));
                SelectionLength = 0;
                CurrentMode = Mode.Select;
            }

            Invalidate();
        }

        private void WaveDisplay_MouseUp(object sender, MouseEventArgs e) {
            CurrentMode = Mode.Idle;
            Invalidate();
        }

        private void WaveDisplay_Paint(object sender, PaintEventArgs e) {
            Render(e.Graphics);
        }

        private long GetSampleIndexFromVisiblePoint(long p) {
            var sigArea = GetSignalArea();
            return (p - sigArea.X) + PinchedSampleOffset;
        }

        private void LimitSelection() {
            if (SelectionStart < 0) SelectionStart = 0;
            if ((SelectionStart + SelectionLength) >= PinchedDataLength) {
                SelectionLength = (PinchedDataLength - SelectionStart);
            }
        }

        private void LimitSampleOffset(Rectangle sigArea) {
            if (PinchedSampleOffset < 0) {
                PinchedSampleOffset = 0;
            } else if (PinchedSampleOffset >= PinchedDataLength - sigArea.Width) {
                PinchedSampleOffset = PinchedDataLength - sigArea.Width;
            }
        }

        private double MapToInterval(double value, Interval from, Interval to) {
            return (value - from.Bottom) / from.Width * to.Width + to.Bottom;
        }

        private string SampleIndexToTimeString(long index) {
            if (Renderer != null && Math.Abs(Renderer.Samplerate) > 0.0001) {
                var seconds = index / Renderer.Samplerate * PinchFactor;
                var ts = TimeSpan.FromSeconds(seconds);
                return $"{ts.Minutes:D2}:{ts.Seconds:D2}:{ts.Milliseconds:D3}";
            }
            return "";
        }

        private void WaveDisplay_Load(object sender, EventArgs e) {

        }

        private void WaveDisplay_Click(object sender, EventArgs e) {
            
        }
    }

    class Interval {

        public double Top { get; set; }
        public double Bottom { get; set; }

        public double Width => Top - Bottom;

        public Interval(double bottom, double top) {
            Top = top;
            Bottom = bottom;
        }

    }
}
