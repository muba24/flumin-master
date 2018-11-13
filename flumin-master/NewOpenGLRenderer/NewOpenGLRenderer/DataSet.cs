using NodeSystemLib2;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NewOpenGLRenderer {

    public class FrequencyDataSet : DataSet {

        public double SamplesPerSecond { get; set; }

        public double FftSize { get; set; }

        public FrequencyDataSet() {
            AxisX.LabelProvider = x => $"{(Math.Round(x * (SamplesPerSecond / FftSize / 2), 1)).ToString(System.Globalization.CultureInfo.InvariantCulture)} Hz";
        }

    }

    public class TimeDataSet : DataSet {

        public int SamplesPerSecond { get; set; }

        public double Milliseconds { get; set; }

        public bool AlignLines { get; set; } = true;

        public TimeStamp CurrentTimeOffset { get; private set; }

        public TimeDataSet() {
            AxisX.LabelProvider = x => TimeSpan.FromSeconds(x / (double)SamplesPerSecond).ToString(@"mm\:ss\:fff");
        }

        public void Clear() {
            foreach (var line in Data) {
                line.Clear();
            }
        }

        public void SetTimeOffset(TimeStamp time) {
            if (time > CurrentTimeOffset) {
                CurrentTimeOffset = time;
            }
        }

        public TimeStamp GetCurrentTimeReference() {
            var inputs = Data.Where(d => d.Visible).OfType<ITimeData>();
            if (!inputs.Any()) return TimeStamp.Zero;

            // 0. if there are 1D signals, sync to them
            // TODO: feels like a bad idea
            if (inputs.Any(i => i is DataLine1D)) {
                inputs = inputs.OfType<DataLine1D>();
            }

            // 1. get biggest length
            ITimeData biggestData = inputs.First();
            foreach (var input in inputs) {
                if (input.Duration.Begin.AsSeconds() > biggestData.Duration.Begin.AsSeconds()) {
                    biggestData = input;
                }
            }

            // 2. get its time point at 0
            return AlignLines ? biggestData.Duration.Begin : TimeStamp.Zero;
        }
        
        public override void Render() {
            GL.UseProgram(_shader.Program);

            DrawGridLines();

            DrawSelection();

            var inputs = Data.Where(d => d.Visible).OfType<ITimeData>();
            if (!inputs.Any()) return;

            //// 0. if there are 1D signals, sync to them
            //// TODO: feels like a bad idea
            //if (inputs.Any(i => i is DataLine1D)) {
            //    inputs = inputs.OfType<DataLine1D>();
            //}

            //// 1. get biggest length
            //ITimeData biggestData = inputs.First();
            //foreach (var input in inputs) {
            //    if (input.Duration.Begin.AsSeconds() > biggestData.Duration.Begin.AsSeconds()) {
            //        biggestData = input;
            //    }
            //}

            //// 2. get its time point at 0
            //var timeRef = AlignLines ? biggestData.Duration.Begin : TimeStamp.Zero();

            var timeRef = GetCurrentTimeReference();

            // 3. use as reference time point to translate others to
            foreach (var data in Data.Where(d => d.Visible).OfType<ITimeData>()) {
                var factor = data.SamplesPerSecond / (double)SamplesPerSecond;
            
                var viewRect = new RectangleF(
                    x: (float)((AxisX.VisibleMinimum) * factor),
                    y: (float)(AxisY.VisibleMaximum),
                    width: (float)(Math.Min(Milliseconds * SamplesPerSecond / 1000 - Math.Max(AxisX.VisibleMinimum, 0), AxisX.VisibleMaximum - AxisX.VisibleMinimum) * factor),
                    height: (float)(AxisY.VisibleMinimum - AxisY.VisibleMaximum)
                );

                _shader.SetShaderMatrix(
                    Matrix4.CreateOrthographicOffCenter(
                        left:   viewRect.Left, 
                        right: (float)((AxisX.VisibleMaximum) * factor),
                        bottom: viewRect.Bottom,
                        top:    viewRect.Top,
                        zNear: -1,
                        zFar:   1
                    )
                );

                data.Render(_shader, viewRect, ParentSize, timeRef);
            }
        }

    }

    public class DataSet {

        protected readonly ShaderColorXY _shader;

        public Axis AxisX { get; }
        public Axis AxisY { get; }
        public List<IData> Data = new List<IData>();

        public bool LogScaleY { get; set; }

        public Color SelectionColor { get; set; } = Color.Orange;

        public bool SelectionVisible { get; set; }

        public Size ParentSize { get; set; }

        public RectangleF Selection {
            get {
                return _selectionRect;
            }
            set {
                _selectionRect = value;

                _selection.Clear();
                _selection.AddVertex(_selectionRect.Left, _selectionRect.Top);
                _selection.AddVertex(_selectionRect.Right, _selectionRect.Top);
                _selection.AddVertex(_selectionRect.Right, _selectionRect.Bottom);
                _selection.AddVertex(_selectionRect.Left, _selectionRect.Bottom);
                _selection.IndexFromLength();
                _selection.Reload();
            }
        }

        private RectangleF _selectionRect;

        private readonly VertexFloatBuffer _selection = new VertexFloatBuffer() { DrawMode = BeginMode.Quads };
        private readonly VertexFloatBuffer _gridLines = new VertexFloatBuffer() { DrawMode = BeginMode.Lines };

        public DataSet() {
            AxisX = new Axis(Axis.AxisOrientation.Horizontal);
            AxisY = new Axis(Axis.AxisOrientation.Vertical);
            _shader = new ShaderColorXY();
        }

        public void Update() {
            AxisX.Update();
            AxisY.Update();
            BuildGridLineBuffer();
        }

        private void BuildGridLineBuffer() {
            _gridLines.Clear();
            foreach (var tick in AxisY.MajorTicks) {
                _gridLines.AddVertex(0, (float)tick);
                _gridLines.AddVertex(1, (float)tick);
            }
            foreach (var tick in AxisX.MajorTicks) {
                _gridLines.AddVertex((float)((tick - AxisX.VisibleMinimum) / AxisX.Range), (float)AxisY.VisibleMinimum);
                _gridLines.AddVertex((float)((tick - AxisX.VisibleMinimum) / AxisX.Range), (float)AxisY.VisibleMaximum);
            }
            _gridLines.IndexFromLength();
            _gridLines.Reload();
        }

        protected void DrawGridLines() {
            _shader.SetShaderMatrix(
                Matrix4.CreateOrthographicOffCenter(
                    0, 1,
                    (float)AxisY.VisibleMinimum,
                    (float)AxisY.VisibleMaximum,
                    -1, 1
                )
            );

            _shader.SetTranslateMatrix(Matrix4.Identity);

            _shader.SetShaderColor(Color.DarkGray);

            GL.LineWidth(1.0f);

            _gridLines.BindAndDraw(_shader);
        }

        protected void DrawSelection() {
            if (SelectionVisible) {
                _shader.SetShaderColor(SelectionColor);
                _selection.BindAndDraw(_shader);
            }
        }

        public virtual void Render() {
            GL.UseProgram(_shader.Program);

            DrawGridLines();

            var viewRect = new RectangleF(
                x:      (float)AxisX.VisibleMinimum,
                y:      (float)AxisY.VisibleMaximum,
                width:  (float)(AxisX.VisibleMaximum - AxisX.VisibleMinimum),
                height: (float)(AxisY.VisibleMinimum - AxisY.VisibleMaximum)
            );

            _shader.SetShaderMatrix(
                Matrix4.CreateOrthographicOffCenter(
                    viewRect.Left, viewRect.Right, viewRect.Bottom, viewRect.Top, -1, 1
                )
            );

            DrawSelection();

            foreach (var data in Data.Where(d => d.Visible)) {
                data.Render(_shader, viewRect, ParentSize, TimeStamp.Zero);
            }
        }

    }

}
