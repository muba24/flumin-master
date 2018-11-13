using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NewOpenGLRenderer {
    
    public class Axis {

        private IEnumerable<double> _minorTicks;
        private IEnumerable<double> _majorTicks;

        public enum AxisOrientation {
            Horizontal, Vertical
        }
        double absoluteMinimum;

        public double AbsoluteMinimum {
            get { return absoluteMinimum; }
            set { absoluteMinimum = value; VisibleMinimum = VisibleMinimum; }
        }

        double absoluteMaximum;

        public double AbsoluteMaximum {
            get { return absoluteMaximum; }
            set { absoluteMaximum = value; VisibleMaximum = VisibleMaximum; }
        }

        private double _visibleMaximum;
        private double _visibleMinimum;

        public double VisibleMaximum {
            get { return _visibleMaximum; }
            set { _visibleMaximum = Math.Min(value, AbsoluteMaximum); }
        }

        public double VisibleMinimum {
            get { return _visibleMinimum; }
            set { _visibleMinimum = Math.Max(value, AbsoluteMinimum); }
        }

        public Func<double, string> LabelProvider { get; set; }
        public AxisOrientation Orientation { get; set; }
        public SizeF Size { get; set; }

        public double Length => Orientation == AxisOrientation.Horizontal ? Size.Width : Size.Height;
        public double Range  => VisibleMaximum - VisibleMinimum;

        public Axis(AxisOrientation orient) {
            LabelProvider  = x => x.ToString(System.Globalization.CultureInfo.InvariantCulture);
            Orientation    = orient;
            AbsoluteMinimum = Double.NegativeInfinity;
            AbsoluteMaximum = Double.PositiveInfinity;
            VisibleMaximum = 1;
            VisibleMinimum = -1;
        }

        public IEnumerable<double> MajorTicks => _majorTicks;
        public IEnumerable<double> MinorTicks => _minorTicks;

        public void Update() {
            var majorTickStep = CalculateActualInterval(Length, 100, Range);
            var minorTickStep = majorTickStep / 5;
            _minorTicks = CreateTickValues(VisibleMinimum, VisibleMaximum, minorTickStep);
            _majorTicks = CreateTickValues(VisibleMinimum, VisibleMaximum, majorTickStep);
        }

        public void Render(Graphics g) {
            Func<double, int> MapToScreenVertical   = x => (int)(Size.Height - ((x - VisibleMinimum) / Range) * Size.Height);
            Action<int> DrawMinorTickVertical       = y => g.DrawLine(Pens.Black, Size.Width - 3, y, Size.Width, y);
            Action<int> DrawMajorTickVertical       = y => g.DrawLine(Pens.Black, Size.Width - 5, y, Size.Width, y);

            Func<double, int> MapToScreenHorizontal = x => (int)((x - (VisibleMinimum)) / (Range) * Size.Width);
            Action<int> DrawMinorTickHorizontal     = x => g.DrawLine(Pens.Black, x, 0, x, 3);
            Action<int> DrawMajorTickHorizontal     = x => g.DrawLine(Pens.Black, x, 0, x, 5);

            Func<double, int> MapToScreen           = x => Orientation == AxisOrientation.Horizontal ? MapToScreenHorizontal(x) : MapToScreenVertical(x);
            Action<int> DrawMinorTick               = x => { if (Orientation == AxisOrientation.Horizontal) DrawMinorTickHorizontal(x); else DrawMinorTickVertical(x); };
            Action<int> DrawMajorTick               = x => { if (Orientation == AxisOrientation.Horizontal) DrawMajorTickHorizontal(x); else DrawMajorTickVertical(x); };

            Action<Graphics, double, int> DrawTickText = (gr, value, x) => {
                if (Orientation == AxisOrientation.Horizontal) DrawTickTextHorizontal(gr, value, x);
                else DrawTickTextVertical(gr, value, x);
            };

            if (_minorTicks == null || _majorTicks == null) return;

            foreach (var tick in _minorTicks) {
                var x = MapToScreen(tick);
                DrawMinorTick(x);
            }

            foreach (var tick in _majorTicks) {
                var x = MapToScreen(tick);
                DrawMajorTick(x);
                DrawTickText(g, tick, x);
            }
        }

        private void DrawTickTextVertical(Graphics g, double value, int y) {
            var str     = LabelProvider(value);
            var strSize = g.MeasureString(str, SystemFonts.CaptionFont);
            g.DrawString(str, SystemFonts.CaptionFont, Brushes.Black, Size.Width - strSize.Width - 8, y - strSize.Height / 2);
        }

        private void DrawTickTextHorizontal(Graphics g, double value, int x) {
            var str     = LabelProvider(value);
            var strSize = g.MeasureString(str, SystemFonts.CaptionFont);
            g.DrawString(str, SystemFonts.CaptionFont, Brushes.Black, x - strSize.Width / 2, 6);
        }

        public double ScreenToPosition(double pixels) {
            if (Orientation == AxisOrientation.Horizontal) {
                return pixels / Length * Range + VisibleMinimum;
            } else {
                return (Length - pixels) / Length * Range + VisibleMinimum;
            }
        }

        // From OxyPlot - Axis.cs
        private static IEnumerable<double> CreateTickValues(double from, double to, double step, int maxTicks = 1000) {
            if (step <= 0) {
                throw new ArgumentException("Step cannot be zero or negative.", nameof(step));
            }

            if (to <= from && step > 0) {
                step *= -1;
            }

            var startValue     = Math.Round(from / step) * step;
            var epsilon        = step * 1e-3 * Math.Sign(step);

            for (int k = 0; k < maxTicks; k++) {
                var lastValue = startValue + (step * k);

                // If we hit the maximum value before reaching the max number of ticks, exit
                if (lastValue > to + epsilon) {
                    break;
                }

                // try to get rid of numerical noise
                var v = Math.Round(lastValue / step, 14) * step;
                yield return v;
            }
        }

        private double CalculateActualInterval(double availableSize, double maxIntervalSize, double range) {
            if (availableSize <= 0) {
                return maxIntervalSize;
            }

            if (Math.Abs(maxIntervalSize) < double.Epsilon) {
                throw new ArgumentException("Maximum interval size cannot be zero.", nameof(maxIntervalSize));
            }

            if (Math.Abs(range) < double.Epsilon) {
                throw new ArgumentException("Range cannot be zero.", nameof(range));
            }

            Func<double, double> exponent = x => Math.Ceiling(Math.Log(x, 10));
            Func<double, double> mantissa = x => x / Math.Pow(10, exponent(x) - 1);

            double maxIntervalCount  = availableSize / maxIntervalSize;

            range                    = Math.Abs(range);
            double interval          = Math.Pow(10, exponent(range));
            double intervalCandidate = interval;

            // Function to remove 'double precision noise'
            Func<double, double> removeNoise = x => double.Parse(x.ToString("e14"));

            // decrease interval until interval count becomes less than maxIntervalCount
            while (true) {
                var m = (int)mantissa(intervalCandidate);
                if (m == 5) {
                    intervalCandidate = removeNoise(intervalCandidate / 2.5);
                } else if (m == 2 || m == 1 || m == 10) {
                    intervalCandidate = removeNoise(intervalCandidate / 2.0);
                } else {
                    intervalCandidate = removeNoise(intervalCandidate / 2.0);
                }

                if (range / intervalCandidate > maxIntervalCount) {
                    break;
                }

                if (double.IsNaN(intervalCandidate) || double.IsInfinity(intervalCandidate)) {
                    break;
                }

                interval = intervalCandidate;
            }

            return interval;
        }


    }

}
