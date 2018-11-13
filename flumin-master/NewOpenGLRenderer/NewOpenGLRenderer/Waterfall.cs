using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NewOpenGLRenderer {
    class Waterfall : IDisposable {

        public struct PointInfo {
            public double Frequency { get; }
            public double Amplitude { get; }
            public PointInfo(double f, double a) {
                Frequency = f;
                Amplitude = a;
            }
        }

        Color[] PaletteColorPoints = {
            Color.Navy, Color.Blue, Color.Red, Color.White
        };

        int[] PaletteSpacings = {
            0, 50, 180, 256
        };


        FFTW.FFTWTransform _fft;
        ScrollTexture _bitmap;
        Queue<FFTFrame> _frames;
        Color[] _palette;
        int fftCapacity;
        int fftSize;
        int samplerate;
        int[] colorBuffer;

        public Waterfall(int fftsize, int ffts, int samplerate) {
            CreatePalette();
            this.fftSize = fftsize;
            this.fftCapacity = ffts;
            this.samplerate = samplerate;
            Init();
        }

        public Waterfall(int samplerate) : this(512, 100, samplerate) {
        }

        void CreatePalette() {
            _palette = GetGradients(PaletteColorPoints, PaletteSpacings).ToArray();
        }

        public float DbMin { get; set; } = -100;
        public float DbMax { get; set; } = 0;

        public int FFTSize {
            get {
                return fftSize;
            }

            set {
                fftSize = value;
                Init();
            }
        }

        public int FFTCapacity {
            get {
                return fftCapacity;
            }

            set {
                fftCapacity = value;
                Init();
            }
        }

        public PointInfo InfoFromPoint(Point p) {
            if (p.X < 0 || p.X >= _frames.Count) return new PointInfo(-1, -1);
            var frame = _frames.ElementAt(p.X);
            var freq = p.Y * frame.Samplerate / (double)frame.FFTSize;
            if (p.Y < 0 || p.Y >= frame.Data.Length) return new PointInfo(-1, -1);
            var ampl = frame.Data[p.Y];
            return new PointInfo(freq, ampl);
        }

        IEnumerable<Color> GetGradients(Color[] colors, int[] steps) {
            if (steps.Length != colors.Length) throw new IndexOutOfRangeException();

            for (int i = 0; i < colors.Length - 1; i++) {
                foreach (var color in GetGradients(colors[i], colors[i + 1], steps[i + 1] - steps[i])) {
                    yield return color;
                }
            }
        }

        IEnumerable<Color> GetGradients(Color start, Color end, int steps) {
            var stepper = new Tuple<sbyte, sbyte, sbyte, sbyte>(
                (sbyte)((end.A - start.A) / (steps - 1)),
                (sbyte)((end.R - start.R) / (steps - 1)),
                (sbyte)((end.G - start.G) / (steps - 1)),
                (sbyte)((end.B - start.B) / (steps - 1))
            );

            for (int i = 0; i < steps; i++) {
                yield return Color.FromArgb(
                    start.A + (stepper.Item1 * i),
                    start.R + (stepper.Item2 * i),
                    start.G + (stepper.Item3 * i),
                    start.B + (stepper.Item4 * i)
                );
            }
        }

        public void DrawTo(RectangleF rc) {
            _bitmap.Draw(rc);
        }

        public void AddFrame(double[] data, int offset, int size) {
            if (size != FFTSize / 2) throw new ArgumentException();

            var frame = _frames.Dequeue();
            Array.Copy(data, frame.Data, FFTSize / 2);
            _frames.Enqueue(frame);

            BitmapAddFrame(frame);
        }

        void CreateFrameBuffer() {
            _frames = new Queue<FFTFrame>(FFTCapacity);
            for (int i = 0; i < FFTCapacity; i++) {
                var frame = new FFTFrame(FFTSize, samplerate);
                _frames.Enqueue(frame);
            }
        }

        private void Init() {
            _fft?.Dispose();
            _bitmap?.Dispose();
            _fft = new FFTW.FFTWTransform(FFTSize);
            _bitmap = new ScrollTexture(FFTCapacity, FFTSize / 2);
            colorBuffer = new int[FFTSize / 2];
            CreateFrameBuffer();
            DrawAllFramesToBitmap();
        }

        void BitmapAddFrame(FFTFrame frame) {
            DrawFrame(frame);
        }

        void DrawAllFramesToBitmap() {
            foreach (var frame in _frames) {
                DrawFrame(frame);
            }
        }

        void DrawFrame(FFTFrame frame) {
            var len = _bitmap.Height;
            var dat = frame.Data;
            
            float factor = 1 / (DbMax - DbMin) * 255;

            for (int y = 0; y < len; y++) {
                var db = 10 * Math.Log(Math.Sqrt(dat[y]));
                var v = (int)((db - DbMin) * factor);
                if (v < 0) v = 0;
                else if (v > 255) v = 255;

                colorBuffer[len - y - 1] = _palette[v].ToArgb();
            }

            _bitmap.AddFrame(colorBuffer);
        }

        public void Dispose() {
            _bitmap.Dispose();
            _fft.Dispose();
        }
    }
}
