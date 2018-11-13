using System;
using System.Linq;
using System.Drawing;

namespace WaveDisplayControl {
    public class TimeDataYScaleRenderer : IYScaleProvider {
        public double YMin { get; private set; }
        public double YMax { get; private set; }
        public double YDelta => YMax - YMin;

        public TimeDataYScaleRenderer(double ymin, double ymax) {
            YMin = ymin;
            YMax = ymax;
        }

        public bool ValidateAndSetYMin(double yMin) {
            YMin = yMin;
            return true;
        }

        public bool ValidateAndSetYMax(double yMax) {
            YMax = yMax;
            return true;
        }

        public void Render(Graphics g, Rectangle area) {
            var fontHeight = g.MeasureString("0", SystemFonts.CaptionFont).Height;
            var stepSize = ((2 * fontHeight) / (area.Height - fontHeight)) * YDelta;

            if (stepSize < 0.0001) return;

            for (var step = YMin; step <= YMax; step += stepSize) {
                g.DrawLine(
                    Pens.Black,
                    new Point(area.Left + area.Width - 3, MapYToRect(step, area)),
                    new Point(area.Left + area.Width - 0, MapYToRect(step, area))
                );

                var strStep = step.ToString("0.00");
                var strSize = g.MeasureString(strStep, SystemFonts.CaptionFont);

                g.DrawString(
                    strStep,
                    SystemFonts.CaptionFont,
                    SystemBrushes.ControlText,
                    new PointF(area.Right - strSize.Width - 3, MapYToRect(step, area) - strSize.Height / 2)
                );
            }

        }

        private int MapYToRect(double y, Rectangle r) {
            return r.Top + (int)(r.Height - (y - YMin) / YDelta * r.Height);
        }
    }

    public class TimeDataRenderer : IDataRenderer {

        public IWaveData Data { get; }

        public TimeDataRenderer(IWaveData data) {
            Data = data;
        }

        public double Samplerate => Data.Samplerate;

        public long DataLength => Data.Length;

        public IYScaleProvider YScale { get; } = new TimeDataYScaleRenderer(-10, 10);

        public void Render(Graphics g, Rectangle area, long start, long count, long step) {
            var points = Data.Iterate(start, area.Width, step)
                             .Select((sample, index) => new Point(
                                 index + area.Left,
                                 MapYToRect(sample, YScale.YMin, YScale.YMax - YScale.YMin, area))
                              );

            var enumerable = points as Point[] ?? points.ToArray();
            var pt = enumerable.FirstOrDefault();

            foreach (var pt2 in enumerable.Skip(1)) {
                g.DrawLine(Pens.Blue, pt, pt2);
                pt = pt2;
            }
        }

        private int MapYToRect(double y, double ymin, double ydelta, Rectangle r) {
            var result = r.Top + (int)(r.Height - (y - ymin) / ydelta * r.Height);
            return Math.Max(Math.Min(r.Bottom, result), r.Top);
        }

        public void Resize(int width, int height) {
            //
        }
    }
}
