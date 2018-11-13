using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace NewOpenGLRenderer {
    public partial class Form1 : Form {

        const float X_MIN = 0;
        const float X_MAX = 300000;
        const float Y_MIN = -1.1f;
        const float Y_MAX = 1.1f;

        private TimeDataSet _dataSet;
        private DataLine1D _lines1;
        private DataLine1D _lines2;
        private DataLine1D _lines3;
        private DataLine2D _linesd;

        private DataLine2D _linesFFT;

        //private float[] data;
        //private float[] data2;
        //private float[] data3;

        private NodeSystemLib2.FormatData1D.TimeLocatedBuffer1D<double> data;
        private NodeSystemLib2.FormatData1D.TimeLocatedBuffer1D<double> data2;
        private NodeSystemLib2.FormatData1D.TimeLocatedBuffer1D<double> data3;

        public Form1() {
            InitializeComponent();

            this.ResizeBegin += (s, e) => { this.SuspendLayout(); };
            this.ResizeEnd += (s, e) => { this.ResumeLayout(true); };
        }

        private void FillVBO(double phaseFactor) {
            var task1 = Task.Run(() => {
                var w1 = 2 * Math.PI * 20 / _lines1.SamplesPerSecond;
                var p1 = phaseFactor;
                var samples = data.Data;
                for (int i = 0; i < data.Capacity; i++) {
                    samples[i] = (float)Math.Sin(p1 += w1);
                }
                data.SetWritten(data.Capacity);
                _lines1.Add(data);
            });

            var task2 = Task.Run(() => {
                var w2 = 2 * Math.PI * 1 / _lines2.SamplesPerSecond;
                var p2 = phaseFactor;
                var samples = data2.Data;
                for (int i = 0; i < data2.Capacity; i++) {
                    samples[i] = (float)Math.Cos(p2 += w2);
                }
                data2.SetWritten(data2.Capacity);
                _lines2.Add(data2);
            });

            var task3 = Task.Run(() => {
                var w3 = 2 * Math.PI * 20 / _lines3.SamplesPerSecond;
                var p3 = phaseFactor;
                var samples = data3.Data;
                for (int i = 0; i < data3.Capacity; i++) {
                    samples[i] = (float)Math.Cos(p3 += w3);
                }
                data3.SetWritten(data3.Capacity);
                _lines3.Add(data3);
            });

            Task.WaitAll(task1, task2, task3);

            _dataSet.SetTimeOffset(data.Time);
            _dataSet.SetTimeOffset(data2.Time);
            _dataSet.SetTimeOffset(data3.Time);
        }

        private void plot2_GLLoaded(object sender, EventArgs e) {
            _lines1 = new DataLine1D("Line 1", (int)X_MAX, (int)X_MAX) { LineColor = Color.Yellow };
            _lines2 = new DataLine1D("Line 2", (int)X_MAX / 2,  (int)X_MAX / 2) { LineColor = Color.Red };
            _lines3 = new DataLine1D("Line 3", (int)X_MAX, (int)X_MAX) { LineColor = Color.Blue };
            _linesd = new DataLine2D("Line D", 1000) { LineColor = Color.DarkViolet, SamplesPerSecond = 1000000 };

            //data  = new float[(int)_lines1.SamplesPerSecond / 20];
            //data2 = new float[(int)_lines2.SamplesPerSecond / 20];
            //data3 = new float[(int)_lines3.SamplesPerSecond / 20];

            data  = new NodeSystemLib2.FormatData1D.TimeLocatedBuffer1D<double>(_lines1.SamplesPerSecond / 10, _lines1.SamplesPerSecond);
            data2 = new NodeSystemLib2.FormatData1D.TimeLocatedBuffer1D<double>(_lines2.SamplesPerSecond / 10, _lines2.SamplesPerSecond);
            data3 = new NodeSystemLib2.FormatData1D.TimeLocatedBuffer1D<double>(_lines3.SamplesPerSecond / 10, _lines3.SamplesPerSecond);

            //var buffer = new float[1024];
            //for (int i = 0; i < buffer.Length; i++) {
            //    buffer[i] = i % 16 + (i % 12) / 10 + 2 * (float)Math.Sin(2 * Math.PI * 224 / 1000.0 * i);
            //}

            //var dft = DFT(buffer);

            //_linesFFT = new DataLine2D();
            //_linesFFT.AddRange(dft.Select((i, d) => new PointF(d, i)));
            //_linesFFT.LineColor = Color.Blue;

            //_dataSet = new FrequencyDataSet { SamplesPerSecond = 1000, FftSize = 1024 };
            //_dataSet.Data.Add(_linesFFT);

            //_dataSet.AxisX.AbsoluteMinimum = 0;
            //_dataSet.AxisX.AbsoluteMaximum = _linesFFT.Length;

            _dataSet = new TimeDataSet() { SamplesPerSecond = (int)X_MAX, Milliseconds = 10000 };
            _dataSet.AxisX.VisibleMinimum = X_MIN;
            _dataSet.AxisX.VisibleMaximum = X_MAX;
            _dataSet.AxisY.VisibleMinimum = Y_MIN;
            _dataSet.AxisY.VisibleMaximum = Y_MAX;

            _dataSet.Data.Add(_lines1);
            _dataSet.Data.Add(_lines2);
            _dataSet.Data.Add(_lines3);
            _dataSet.Data.Add(_linesd);

            for (int i = 0; i <= 3; i++) {
                _linesd.Add(new PointF(i * 1000000 / 3f, i == 0 ? 1 : 1f / (i * i)));
            }

            FillVBO(0);

            plot2.Set = _dataSet;
            plot2.CreateLegend();
        }

        double phase = 0;
        int tmrCount = 0;
        Random rnd = new Random();

        private void tmrPhase_Tick(object sender, EventArgs e) {
            FillVBO(phase += 0.1);
            
            tmrCount = (tmrCount + 1) % 10;
            if (tmrCount == rnd.Next(10)) {
                _linesd.Add(new PointF((float)_dataSet.CurrentTimeOffset.ToRate(_linesd.SamplesPerSecond), (float)rnd.NextDouble()));
                //System.Diagnostics.Debug.WriteLine("Emit");
            }

            plot2.Refresh();
        }

        private float[] DFT(float[] buffer) {
            var result = new float[buffer.Length / 2];
            for (int i = 0; i < buffer.Length / 2; i++) {
                var im = 0.0;
                var re = 0.0;
                for (int j = 0; j < buffer.Length; j++) {
                    re += buffer[j] * Math.Cos(i * j * 2 * Math.PI / buffer.Length);
                    im -= buffer[j] * Math.Sin(i * j * 2 * Math.PI / buffer.Length);
                }
                result[i] = (float)Math.Sqrt(re * re + im * im) / buffer.Length * 2;
            }
            return result;
        }
    }
}
