using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;

namespace WaveDisplayControl {
    public class FftDataYScaleRenderer : IYScaleProvider {

        public double YMin { get; private set; }
        public double YMax { get; private set; }
        public double YDelta => YMax - YMin;

        public FftDataYScaleRenderer(double ymin, double ymax) {
            YMin = ymin;
            YMax = ymax;
        }

        public bool ValidateAndSetYMin(double yMin) {
            if (yMin < 0) return false;
            YMin = yMin;
            return true;
        }

        public bool ValidateAndSetYMax(double yMax) {
            if (yMax < 0) return false;
            YMax = yMax;
            return true;
        }

        public void Render(Graphics g, Rectangle area) {
            var fontHeight = g.MeasureString("0", SystemFonts.CaptionFont).Height;
            var stepSize = ((2 * fontHeight) / (area.Height - fontHeight)) * YDelta;

            if (stepSize < 0.00001) return;

            for (var step = YMin; step <= YMax; step += stepSize) {
                g.DrawLine(
                    Pens.Black,
                    new Point(area.Left + area.Width - 3, MapYToRect(step, area)),
                    new Point(area.Left + area.Width - 0, MapYToRect(step, area))
                );

                var strStep = Math.Round(step/2/1000, 2) + "k";
                var strSize = g.MeasureString(strStep, SystemFonts.CaptionFont);

                g.DrawString(
                    strStep,
                    SystemFonts.CaptionFont,
                    SystemBrushes.ControlText,
                    new PointF(area.Right - strSize.Width - 3, MapYToRect(step, area) - strSize.Height / 2)
                );
            }
        }

        private double HeightToHerz(double height, Rectangle r) {
            return r.Height/height * YMax;
        }

        private int MapYToRect(double y, Rectangle r) {
            return r.Top + (int)(r.Height - (y - YMin) / YDelta * r.Height);
        }

    }

    public class FftDataRenderer : IDataRenderer {

        private Bitmap Bmp { get; set; }

        public FftData Data {
            get { return _data; }
            set {
                _data = value;
                YScale.ValidateAndSetYMax(_data.Samplerate);
            }
        }

        public Color[] Palette { get; } = new Color[byte.MaxValue + 1];

        public double Samplerate => Data.Samplerate;

        public long DataLength => Data.Length;

        public IYScaleProvider YScale { get; } = new FftDataYScaleRenderer(0, 10000);

        public FftDataRenderer(FftData data) {
            Data = data;
            InitializePalette();
            YScale.ValidateAndSetYMax(Data.Samplerate);
        }

        private void InitializePalette() {
            for (var i = 0; i < Palette.Length; i++) {
                Palette[i] = Color.FromArgb(255 - i,
                                            255 - i,
                                            255 - i);
            }
        }

        private readonly object _resizeLock = new object();
        private FftData _data;

        public void Resize(int width, int height) {
            lock (_resizeLock) {
                if (height > 0 && width > 0) {
                    Bmp = new Bitmap(width, height, PixelFormat.Format24bppRgb);
                } else {
                    Bmp?.Dispose();
                    Bmp = null;
                }
            }
        }

        public void Render(Graphics g, Rectangle area, long start, long count, long step) {
            if (Bmp == null) return;

            var spectrums = Data.Iterate(start, area.Width, step);

            var x = 0;
            const int pixelSize = 3;

            var bmd = Bmp.LockBits(new Rectangle(0, 0, Bmp.Width, Bmp.Height), ImageLockMode.ReadWrite, Bmp.PixelFormat);

            unsafe
            {
                foreach (var spectrum in spectrums) {
                    var skip = spectrum.Length / (double)area.Height / 2;

                    if (spectrum.Length > 0) {
                        var row = (byte*)bmd.Scan0;

                        var yy = area.Height - 1;
                        for (var y = 0; y < bmd.Height - 1; y++) {
                            var idx = (int)(yy-- * skip);
                            if (idx < 0) idx = 0;
                            if (idx >= spectrum.Length) idx = spectrum.Length - 1;

                            var value = (int)(255 * spectrum[idx]);
                            if (value > 255) value = 255;
                            if (value < 0) value = 0;

                            var pixelColor = Palette[value];
                            row[x + 0] = pixelColor.B;
                            row[x + 1] = pixelColor.G;
                            row[x + 2] = pixelColor.R;

                            row += bmd.Stride;
                        }
                    }
                    x += pixelSize;
                }

            }

            Bmp.UnlockBits(bmd);

            g.DrawImage(Bmp, area.Location);
        }

    }
}
