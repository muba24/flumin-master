using OpenTK;
using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using NodeSystemLib2;
using NodeSystemLib2.FileFormats;
using NodeSystemLib2.Generic;

namespace NewOpenGLRenderer {

    public class DataLineFile2D : ITimeData, IDisposable {

        private TimeStamp _end;
        private TimeStamp _begin;
        private Stream2DReader _reader;

        private readonly VertexFloatBuffer _vbo;

        public bool Visible { get; set; } = true;

        public int SamplesPerSecond { get; set; } = 1000000;

        public Color LineColor { get; set; }

        public bool Selected { get; set; }

        public TimeInterval Duration => new TimeInterval(_begin, _end);

        public string Name { get; set; }

        public void Clear() {
            //
        }

        public DataLineFile2D(string name, Stream2DReader reader, TimeStamp begin, TimeStamp end) {
            _reader = reader;
            _begin = begin;
            _end = end;

            Name = name;

            _vbo = new VertexFloatBuffer();
            _vbo.DrawMode = BeginMode.LineStrip;
            _vbo.IndexFromLength();
            _vbo.Load();

            ReadFile();
        }

        public void Render(ShaderColorXY shader, RectangleF view, Size areaInPixels, TimeStamp offset) {
            if (_vbo.Length == 0) return;

            // TODO: this should be done in Add
            //       as the buffer will be reloaded for every frame.
            //       Not much data to be copied, but still unnecessary.
            var lastVertex = _vbo[_vbo.Length - 1];
            _vbo.AddVertex(float.MaxValue, lastVertex.Y);
            _vbo.IndexFromLength();
            _vbo.Reload();

            shader.SetTranslateMatrix(Matrix4.Identity);
            shader.SetShaderColor(LineColor);

            var trsl = Matrix4.CreateTranslation(-offset.ToRate(SamplesPerSecond) , 0, 0);
            shader.SetTranslateMatrix(trsl);

            GL.LineWidth(Selected ? 3.0f : 1.0f);
            _vbo.DrawMode = BeginMode.LineStrip;
            _vbo.BindAndDraw(shader);

            GL.PointSize(Selected ? 8.0f : 6.0f);
            _vbo.DrawMode = BeginMode.Points;
            _vbo.BindAndDraw(shader);

            _vbo.PopVertex();
        }

        private void ReadFile() {
            while (true) {
                NodeSystemLib2.FormatValue.TimeLocatedValue<double> value;
                if (!_reader.ReadSample(out value)) break;
                _vbo.AddVertex(value.Stamp.ToRate(SamplesPerSecond), (float)value.Value);
            }
        }

        public void Dispose() {
            _reader?.Dispose();
        }
    }

    public class DataLine2D : ITimeData {

        private readonly VertexFloatBuffer _vbo;

        private bool _needsReload;

        private TimeStamp _currentTime;

        private bool _firstSample;
        private float _lastY;

        private double _millis;

        public DataLine2D(string name, double milliWindow) {
            _vbo = new VertexFloatBuffer();
            _vbo.DrawMode = BeginMode.LineStrip;
            _vbo.IndexFromLength();
            _vbo.Load();
            _millis = milliWindow;
            Name = name;
            _firstSample = true;
        }

        public bool Visible { get; set; } = true;

        public int SamplesPerSecond { get; set; }

        public Color LineColor { get; set; }

        public bool Selected { get; set; }

        public TimeInterval Duration {
            get {
                return new TimeInterval(new TimeStamp((_currentTime - new TimeStamp(_millis)).AsSeconds()), _currentTime);
            }
        }

        public string Name { get; set; }

        public void Clear() {
            _vbo.Clear();
            _currentTime = TimeStamp.Zero;
            _needsReload = true;
            _firstSample = true;
        }

        public void Add(PointF sample) {
            if (!_firstSample) {
                _vbo.AddVertex(sample.X, _lastY);
            }
            _firstSample = false;
            _vbo.AddVertex(sample.X, sample.Y);
            _lastY = sample.Y;
            if (sample.X > _currentTime.ToRate(SamplesPerSecond)) {
                _currentTime = new TimeStamp(1000 * sample.X / SamplesPerSecond);
            }
            _needsReload = true;
        }

        public void AddRange(IEnumerable<PointF> samples) {
            foreach (var sample in samples) Add(sample);
            _needsReload = true;
        }

        public void Render(ShaderColorXY shader, RectangleF view, Size areaInPixels, TimeStamp offset) {
            if (_vbo.Length == 0) return;

            // TODO: this should be done in Add
            //       as the buffer will be reloaded for every frame.
            //       Not much data to be copied, but still unnecessary.
            var lastVertex = _vbo[_vbo.Length - 1];
            _vbo.AddVertex(float.MaxValue, lastVertex.Y);
            _needsReload = true;

            if (_needsReload) {
                _needsReload = false;
                _vbo.IndexFromLength();
                _vbo.Reload();
            }

            shader.SetTranslateMatrix(Matrix4.Identity);
            shader.SetShaderColor(LineColor);
            
            var trsl = Matrix4.CreateTranslation(-offset.ToRate(SamplesPerSecond) , 0, 0);
            shader.SetTranslateMatrix(trsl);

            GL.LineWidth(Selected ? 3.0f : 1.0f);
            _vbo.DrawMode = BeginMode.LineStrip;
            _vbo.BindAndDraw(shader);

            GL.PointSize(Selected ? 8.0f : 6.0f);
            _vbo.DrawMode = BeginMode.Points;
            _vbo.BindAndDraw(shader, step: 2);

            _vbo.PopVertex();
        }
        
    }


    public class DataLineFile1D : ITimeData, IDisposable {

        private readonly VertexRingBuffer _ringBuffer;
        private readonly VertexRingBuffer _ringBufferMax;
        private readonly VertexRingBuffer _ringBufferMin;

        private readonly int[] _zoomLevels;

        private readonly MinMax[] _minMaxTemp;
        private readonly float[] _floatTemp;
        private readonly double[] _doubleTemp;

        private readonly Stream1DReader _reader;
        private readonly TimeStamp _begin;
        private readonly int _samplerate;

        private MinMaxCacheFirstStage _minMaxFst;
        private IMinMaxCache[] _minMax;
        private int _level;

        public void Clear() {
            //
        }

        public DataLineFile1D(string name, Stream1DReader reader, int samplerate, TimeStamp begin) {
            _zoomLevels = new [] { 64, 128, 256, 512, 1024, 2048, 4096, 8192, 2*8192 };

            var biggestScreen = System.Windows.Forms.Screen.AllScreens.Max(screen => screen.WorkingArea.Size);
            var bufferSize = biggestScreen.Width * _zoomLevels.First();

            _begin      = begin;
            _reader     = reader;
            _samplerate = samplerate;
            _floatTemp  = new float[bufferSize];
            _doubleTemp = new double[bufferSize];
            _minMaxTemp = new MinMax[bufferSize];
            _ringBuffer = new VertexRingBuffer(bufferSize);
            _ringBufferMax = new VertexRingBuffer(bufferSize);
            _ringBufferMin = new VertexRingBuffer(bufferSize);

            Name = name;

            ReadFile();
        }

        public TimeInterval Duration => new TimeInterval(_begin, _begin.Increment(_reader.SampleCount, _samplerate));

        public Color LineColor { get; set; }

        public int SamplesPerSecond => _samplerate;

        public bool Selected { get; set; }

        public bool Visible {get;set;}

        public string Name {get;set;}

        /// <summary>
        /// Fetch data from input buffer to OpenGL buffer
        /// </summary>
        /// <param name="level">level in MinMax cascade. -1 being raw data, 0..N being MinMax data</param>
        /// <param name="offset">Offset in source buffer in samples</param>
        /// <param name="samples">Number of samples to copy from source buffer</param>
        /// <returns></returns>
        private int FillBuffer(int level, long offset, int samples) {
            if (level < -1 || level > _minMax.Length) {
                throw new IndexOutOfRangeException($"Level must be in range [-1..{_minMax.Length - 1}]");
            }

            if (level == -1) {
                // read from file
                _reader.Seek(offset, System.IO.SeekOrigin.Begin);
                var read = _reader.ReadSamples(_doubleTemp, 0, samples);
                for (int i = 0; i < read; i++) {
                    _floatTemp[i] = (float)_doubleTemp[i];
                }
                _ringBuffer.ResetPointer();
                _ringBuffer.AddVertices(_floatTemp, 0, (int)read);
                return (int)read;

            } else {
                var c = Math.Min(samples, _minMax[level].Samples.Length - offset);
                if (c < 0) c = 0;
                _minMax[level].Samples.Peek(_minMaxTemp, (int)offset, 0, (int)c);
                for (int i = 0; i < 2 * c; i += 2) {
                    _floatTemp[i + 0] = _minMaxTemp[i >> 1].Max;
                    _floatTemp[i + 1] = _minMaxTemp[i >> 1].Min;
                }
                _ringBuffer.ResetPointer();
                _ringBuffer.AddVertices(_floatTemp, 0, 2*(int)c);

                for (int i = 0; i < c; i++) {
                    _floatTemp[i] = _minMaxTemp[i].Max;
                }
                _ringBufferMax.ResetPointer();
                _ringBufferMax.AddVertices(_floatTemp, 0, (int)c);

                for (int i = 0; i < c / 2; i++) {
                    _floatTemp[i] = _minMaxTemp[i].Min;
                }
                _ringBufferMin.ResetPointer();
                _ringBufferMin.AddVertices(_floatTemp, 0, (int)c);

                return 2*(int)c;

            }
        }

        public void Render(ShaderColorXY shader, RectangleF view, Size areaInPixels, TimeStamp offset) {
            // find an appropriate zoom level for current view
            var level = -1;
            var zoomFactor = areaInPixels.Width / (double)view.Width;
            for (int i = _zoomLevels.Length - 1; i >= 0; i--) {
                if (zoomFactor < 1.0 / _zoomLevels[i]) {
                    level = i;
                    break;
                }
            }

            if (_level != level) {
                if (level == -1) {
                    SetX(1);
                } else {
                    //SetX(_zoomLevels[level]);
                    SetXTriangles(_zoomLevels[level] / 2);
                }

                _level = level;
            }

            var leftSample = (int)(Math.Max(0, view.Left - Duration.Begin.ToRate(SamplesPerSecond)));

            var sampleCount = FillBuffer(
                level:   level,
                offset:  leftSample / (level == -1 ? 1 : _zoomLevels[level]),
                samples: (int)((Math.Max(0, view.Width)) / (level == -1 ? 1 : _zoomLevels[level]))
            );

            // Render
            GL.BindBuffer(BufferTarget.ArrayBuffer, _ringBuffer.VboX);
            GL.EnableVertexAttribArray(shader.AttributeX);
            GL.VertexAttribPointer(shader.AttributeX, 1, VertexAttribPointerType.Float, false, 4, 0);

            GL.BindBuffer(BufferTarget.ArrayBuffer, _ringBuffer.VboY);
            GL.EnableVertexAttribArray(shader.AttributeY);
            GL.VertexAttribPointer(shader.AttributeY, 1, VertexAttribPointerType.Float, false, 4, 0);

            GL.BindBuffer(BufferTarget.ElementArrayBuffer, _ringBuffer.Ebo);

            shader.SetShaderColor(LineColor);

            GL.LineWidth(Selected ? 3.0f : 1.0f);

            var trsl = Matrix4.CreateTranslation(leftSample + Duration.Begin.ToRate(SamplesPerSecond), 0, 0);
            shader.SetTranslateMatrix(trsl);
            GL.DrawElements(BeginMode.LineStrip, sampleCount, DrawElementsType.UnsignedInt, new IntPtr(0));


            if (level != -1) {
                GL.BindBuffer(BufferTarget.ArrayBuffer, _ringBufferMax.VboX);
                GL.EnableVertexAttribArray(shader.AttributeX);
                GL.VertexAttribPointer(shader.AttributeX, 1, VertexAttribPointerType.Float, false, 4, 0);

                GL.BindBuffer(BufferTarget.ArrayBuffer, _ringBufferMax.VboY);
                GL.EnableVertexAttribArray(shader.AttributeY);
                GL.VertexAttribPointer(shader.AttributeY, 1, VertexAttribPointerType.Float, false, 4, 0);

                GL.BindBuffer(BufferTarget.ElementArrayBuffer, _ringBufferMax.Ebo);

                shader.SetShaderColor(LineColor);

                GL.LineWidth(Selected ? 3.0f : 1.0f);

                trsl = Matrix4.CreateTranslation(leftSample + Duration.Begin.ToRate(SamplesPerSecond), 0, 0);
                shader.SetTranslateMatrix(trsl);

                GL.DrawElements(BeginMode.LineStrip, sampleCount / 2 - 1, DrawElementsType.UnsignedInt, new IntPtr(0));



                GL.BindBuffer(BufferTarget.ArrayBuffer, _ringBufferMin.VboX);
                GL.EnableVertexAttribArray(shader.AttributeX);
                GL.VertexAttribPointer(shader.AttributeX, 1, VertexAttribPointerType.Float, false, 4, 0);

                GL.BindBuffer(BufferTarget.ArrayBuffer, _ringBufferMin.VboY);
                GL.EnableVertexAttribArray(shader.AttributeY);
                GL.VertexAttribPointer(shader.AttributeY, 1, VertexAttribPointerType.Float, false, 4, 0);

                GL.BindBuffer(BufferTarget.ElementArrayBuffer, _ringBufferMin.Ebo);

                shader.SetShaderColor(LineColor);

                GL.LineWidth(Selected ? 3.0f : 1.0f);

                trsl = Matrix4.CreateTranslation(leftSample + Duration.Begin.ToRate(SamplesPerSecond), 0, 0);
                shader.SetTranslateMatrix(trsl);

                GL.DrawElements(BeginMode.LineStrip, sampleCount / 2, DrawElementsType.UnsignedInt, new IntPtr(0));
            }
        }

        private void ReadFile() {
            _minMax = MinMaxCache.CreateCascade(_zoomLevels, _reader.SampleCount);
            _minMaxFst = (MinMaxCacheFirstStage)_minMax[0];

            double[] buffer = new double[1000];
            float[] toFloat = new float[buffer.Length];

            var read = 0L;
            while ((read = _reader.ReadSamples(buffer, 0, buffer.Length)) > 0) {
                for (int i = 0; i < read; i++) toFloat[i] = (float)buffer[i];
                _minMaxFst.ConsumeSamples(toFloat, 0, (int)read);
            }

            _reader.Seek(0, System.IO.SeekOrigin.Begin);
        }

        /// <summary>
        /// Set X coordinates for points in GL buffer according to current zoom level in the data set
        /// </summary>
        /// <param name="divider">Samplerate divider</param>
        private void SetX(int divider) {
            for (int i = 0; i < _floatTemp.Length; i++) {
                _floatTemp[i] = i * divider;
            }
            _ringBuffer.SetVerticesX(_floatTemp);
        }

        private void SetXTriangles(int divider) {
            for (int i = 0; i < _floatTemp.Length; i += 2) {
                _floatTemp[i + 0] = i * divider;
                _floatTemp[i + 1] = _floatTemp[i + 0];
            }
            _ringBuffer.SetVerticesX(_floatTemp);

            for (int i = 0; i < _floatTemp.Length; i++) {
                _floatTemp[i] = i * divider * 2;
            }
            _ringBufferMax.SetVerticesX(_floatTemp);
            _ringBufferMin.SetVerticesX(_floatTemp);
        }

        public void Dispose() {
            _reader?.Dispose();
        }
    }


    public class DataLine1D : ITimeData {

        private IMinMaxCache[] _minMax;

        private MinMaxCacheFirstStage _minMaxFst;

        private readonly GenericRingBuffer<float> _signalBuffer;

        private readonly VertexRingBuffer _ringBuffer;
        private readonly VertexRingBuffer _ringBufferMax;
        private readonly VertexRingBuffer _ringBufferMin;

        private TimeStamp _currentTime;

        public int SamplesPerSecond { get; }

        public Color LineColor { get; set; }

        public bool Selected { get; set; }

        public bool Visible { get; set; } = true;

        public TimeInterval Duration => new TimeInterval(new TimeStamp((_currentTime - new TimeStamp(_signalBuffer.Length, SamplesPerSecond)).AsSeconds()), _currentTime);

        public string Name { get; set; }

        private int _level;

        private int[] _zoomLevels;

        private float[] _floatTemp;
        private MinMax[] _minMaxTemp;

        /// <summary>
        /// Continuous-time stream of data
        /// </summary>
        /// <param name="size">number of samples the buffer can hold</param>
        public DataLine1D(string name, int size, int samplerate) {
            _zoomLevels = new[] {
                1 << 6,
                1 << 7,
                1 << 8,
                1 << 9,
                1 << 10,
                1 << 11,
                1 << 12,
                1 << 13,
                1 << 14,
                1 << 15,
                1 << 16
            };

            

            _ringBuffer      = new VertexRingBuffer(size);
            _ringBufferMax   = new VertexRingBuffer(size);
            _ringBufferMin   = new VertexRingBuffer(size);
            _floatTemp       = new float[size];
            _minMaxTemp      = new MinMax[size];

            Name = name;

            SamplesPerSecond = samplerate;
            _signalBuffer    = new GenericRingBuffer<float>(size) { FixedSize = true };
            _minMax          = MinMaxCache.CreateCascade(_zoomLevels, size);
            _minMaxFst       = (MinMaxCacheFirstStage)_minMax[0];

            // init as -2 because -1 is for fully zoomed in and 0-.. is for other zoom levels.
            // -2 so that in Render() a level change occurs
            _level           = -2;
        }

        //public void SaveState(NodeState state, string key) {
        //    state[key + "levels"] = _zoomLevels.Clone();
        //    state[key + "minmax"] = MinMaxCache.FirstLevelToArray(_minMaxFst);
        //}

        //public void LoadState(NodeState state, string key) {
        //    _zoomLevels = (int[])state[key + "levels"];
        //    _minMax = MinMaxCache.CascadeFromFirstLevelArray((MinMax[])state[key + "minmax"], _zoomLevels);
        //    _minMaxFst = (MinMaxCacheFirstStage)_minMax[0];
        //}

        public void Clear() {
            _signalBuffer.Clear();
            _currentTime = TimeStamp.Zero;
            foreach (var minmax in _minMax) {
                minmax.Clear();
            }
        }

        /// <summary>
        /// Write a buffer of time data to the input queue
        /// </summary>
        /// <param name="buffer">Data to be copied</param>
        /// <remarks>CurrentTime of the data line will be set to the current time of <paramref name="buffer"/></remarks>
        public void Add(NodeSystemLib2.FormatDataFFT.TimeLocatedBufferFFT buffer) {
            var samples = buffer.Data;

            for (int i = 0; i < buffer.FrameSize * buffer.Available; i++) {
                _floatTemp[i] = (float)samples[i];
            }

            Add(_floatTemp, 0, buffer.FrameSize * buffer.Available);
            _currentTime = buffer.Time;
        }

        /// <summary>
        /// Write a buffer of time data to the input queue
        /// </summary>
        /// <param name="buffer">Data to be copied</param>
        /// <remarks>CurrentTime of the data line will be set to the current time of <paramref name="buffer"/></remarks>
        public void Add(NodeSystemLib2.FormatData1D.TimeLocatedBuffer1D<double> buffer) {
            var samples = buffer.Data;

            for (int i = 0; i < buffer.Available; i++) {
                _floatTemp[i] = (float)samples[i];
            }

            Add(_floatTemp, 0, buffer.Available);
            _currentTime = buffer.Time;
        }

        private void Add(float[] samples, int offset, int count) {
            _minMaxFst.ConsumeSamples(samples, offset, count);
            _signalBuffer.Enqueue(samples, offset, count);
        }

        /// <summary>
        /// Fetch data from input buffer to OpenGL buffer
        /// </summary>
        /// <param name="level">level in MinMax cascade. -1 being raw data, 0..N being MinMax data</param>
        /// <param name="offset">Offset in source buffer in samples</param>
        /// <param name="samples">Number of samples to copy from source buffer</param>
        /// <returns></returns>
        private int FillBuffer(int level, int offset, int samples) {
            if (level < -1 || level > _minMax.Length) {
                throw new IndexOutOfRangeException($"Level must be in range [-1..{_minMax.Length - 1}]");
            }

            if (level == -1) {
                var c = Math.Min(samples, _signalBuffer.Length - offset);
                if (c < 0) c = 0;
                _signalBuffer.Peek(_floatTemp, offset, 0, c);
                _ringBuffer.ResetPointer();
                _ringBuffer.AddVertices(_floatTemp, 0, c);
                return c;

            } else {
                var c = Math.Min(samples, _minMax[level].Samples.Length - offset);
                if (c < 0) c = 0;
                _minMax[level].Samples.Peek(_minMaxTemp, offset, 0, c);

                for (int i = 0; i < c; i += 2) {
                    _floatTemp[i + 0] = _minMaxTemp[i >> 1].Max;
                    _floatTemp[i + 1] = _minMaxTemp[i >> 1].Min;
                }
                _ringBuffer.ResetPointer();
                _ringBuffer.AddVertices(_floatTemp, 0, c);

                for (int i = 0; i < c / 2; i++) {
                    _floatTemp[i] = _minMaxTemp[i].Max;
                }
                _ringBufferMax.ResetPointer();
                _ringBufferMax.AddVertices(_floatTemp, 0, c / 2);

                for (int i = 0; i < c / 2; i++) {
                    _floatTemp[i] = _minMaxTemp[i].Min;
                }
                _ringBufferMin.ResetPointer();
                _ringBufferMin.AddVertices(_floatTemp, 0, c / 2);

                return c;

            }
        }

        /// <summary>
        /// Set X coordinates for points in GL buffer according to current zoom level in the data set
        /// </summary>
        /// <param name="divider">Samplerate divider</param>
        private void SetX(int divider) {
            for (int i = 0; i < _floatTemp.Length; i++) {
                _floatTemp[i] = i * divider;
            }
            _ringBuffer.SetVerticesX(_floatTemp);
        }

        private void SetXTriangles(int divider) {
            for (int i = 0; i < _floatTemp.Length; i += 2) {
                _floatTemp[i + 0] = i * divider;
                _floatTemp[i + 1] = _floatTemp[i + 0];
            }
            _ringBuffer.SetVerticesX(_floatTemp);

            for (int i = 0; i < _floatTemp.Length; i++) {
                _floatTemp[i] = i * divider * 2;
            }
            _ringBufferMax.SetVerticesX(_floatTemp);
            _ringBufferMin.SetVerticesX(_floatTemp);
        }

        public void Render(ShaderColorXY shader, RectangleF view, Size areaInPixels, TimeStamp offset) {
            var ringBuffer = _ringBuffer;

            // y per pixel
            var visibleY = -1 * view.Height / areaInPixels.Height;

            // find an appropriate zoom level for current view
            var level = -1;
            var zoomFactor = areaInPixels.Width / (double)view.Width;
            for (int i = _zoomLevels.Length - 1; i >= 0; i--) {
                if (zoomFactor < 1.0 / _zoomLevels[i]) {
                    level = i;
                    break;
                }
            }

            if (_level != level) {
                System.Diagnostics.Debug.WriteLine("Level: " + level);
                if (level == -1) {
                    _ringBuffer.Length = _ringBuffer.Capacity;
                    SetX(1);
                } else {
                    _ringBuffer.Length = _ringBuffer.Capacity / _zoomLevels[level];
                    SetXTriangles(_zoomLevels[level]);
                }

                _level = level;
            }

            // leftSample is the first sample that is visible in the current View.
            // It will be shifted to the left though if it is older than the other signals.
            // This must be corrected.
            // So calculate the left sample index to be the one that sits on the reference time.

            // fill GL buffer with visible samples
            var shift = ((offset - Duration.Begin).AsSeconds() * SamplesPerSecond);
            var leftSample = (int)((Math.Max(0, view.Left)) + shift);

            // Now the sample count is no more correct. We have to subtract what we just added.

            var sampleCount = FillBuffer(
                level:   level, 
                offset:  leftSample / (level == -1 ? 1 : _zoomLevels[level]),
                samples: (int)(Math.Max(0, view.Width + shift) / (level == -1 ? 1 : _zoomLevels[level]))
            );
            

            // Render
            GL.BindBuffer(BufferTarget.ArrayBuffer, ringBuffer.VboX);
            GL.EnableVertexAttribArray(shader.AttributeX);
            GL.VertexAttribPointer(shader.AttributeX, 1, VertexAttribPointerType.Float, false, 4, 0);

            GL.BindBuffer(BufferTarget.ArrayBuffer, ringBuffer.VboY);
            GL.EnableVertexAttribArray(shader.AttributeY);
            GL.VertexAttribPointer(shader.AttributeY, 1, VertexAttribPointerType.Float, false, 4, 0);

            GL.BindBuffer(BufferTarget.ElementArrayBuffer, ringBuffer.Ebo);

            shader.SetShaderColor(LineColor);

            GL.LineWidth(Selected ? 3.0f : 1.0f);

            var trsl = Matrix4.CreateTranslation((float)(leftSample + ((-offset.AsSeconds() + _currentTime.AsSeconds()) * SamplesPerSecond) - _signalBuffer.Length), 0, 0);
            shader.SetTranslateMatrix(trsl);

            if (level == -1) {
                GL.DrawElements(BeginMode.LineStrip, sampleCount, DrawElementsType.UnsignedInt, new IntPtr(0));
            } else {
                GL.DrawElements(BeginMode.TriangleStrip, sampleCount, DrawElementsType.UnsignedInt, new IntPtr(0));
            }

            if (level != -1) {
                GL.BindBuffer(BufferTarget.ArrayBuffer, _ringBufferMax.VboX);
                GL.EnableVertexAttribArray(shader.AttributeX);
                GL.VertexAttribPointer(shader.AttributeX, 1, VertexAttribPointerType.Float, false, 4, 0);

                GL.BindBuffer(BufferTarget.ArrayBuffer, _ringBufferMax.VboY);
                GL.EnableVertexAttribArray(shader.AttributeY);
                GL.VertexAttribPointer(shader.AttributeY, 1, VertexAttribPointerType.Float, false, 4, 0);

                GL.BindBuffer(BufferTarget.ElementArrayBuffer, _ringBufferMax.Ebo);

                shader.SetShaderColor(LineColor);

                GL.LineWidth(Selected ? 3.0f : 1.0f);

                trsl = Matrix4.CreateTranslation((float)(leftSample + ((-offset.AsSeconds() + _currentTime.AsSeconds()) * SamplesPerSecond) - _signalBuffer.Length), 0, 0);
                shader.SetTranslateMatrix(trsl);

                GL.DrawElements(BeginMode.LineStrip, sampleCount / 2, DrawElementsType.UnsignedInt, new IntPtr(0));



                GL.BindBuffer(BufferTarget.ArrayBuffer, _ringBufferMin.VboX);
                GL.EnableVertexAttribArray(shader.AttributeX);
                GL.VertexAttribPointer(shader.AttributeX, 1, VertexAttribPointerType.Float, false, 4, 0);

                GL.BindBuffer(BufferTarget.ArrayBuffer, _ringBufferMin.VboY);
                GL.EnableVertexAttribArray(shader.AttributeY);
                GL.VertexAttribPointer(shader.AttributeY, 1, VertexAttribPointerType.Float, false, 4, 0);

                GL.BindBuffer(BufferTarget.ElementArrayBuffer, _ringBufferMin.Ebo);

                shader.SetShaderColor(LineColor);

                GL.LineWidth(Selected ? 3.0f : 1.0f);

                trsl = Matrix4.CreateTranslation((float)(leftSample + ((-offset.AsSeconds() + _currentTime.AsSeconds()) * SamplesPerSecond) - _signalBuffer.Length), 0, 0);
                shader.SetTranslateMatrix(trsl);

                GL.DrawElements(BeginMode.LineStrip, sampleCount / 2, DrawElementsType.UnsignedInt, new IntPtr(0));
            }
        }
        
    }

}
