using NewOpenGLRenderer;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using WeifenLuo.WinFormsUI.Docking;
using NodeSystemLib2.FormatDataFFT;
using NodeSystemLib2;

namespace MetricTimeDisplay {
    public partial class DisplayFourierWindow : DockContent {

        private Plot _plotCtrl;
        private TimeDataSet _set;
        private bool run;
        private bool _redraw;
        private TimeLocatedBufferFFT _buffer;
        private DataLine1D _line;

        public TimeFourierDisplay ParentMetric { get; set; }

        public DisplayFourierWindow() {
            InitializeComponent();
        }

        public void PrepareProcessing() {
            _buffer = new TimeLocatedBufferFFT(
                1, //DefaultParameters.DefaultBufferMilliseconds.ToFrames(ParentMetric.Samplerate, ParentMetric.FFTSize / 2), 
                ParentMetric.FFTSize, 
                ParentMetric.Samplerate
            );

            _line = new DataLine1D("", ParentMetric.FFTSize / 2, ParentMetric.Samplerate);
            _set.Data.Clear();
            _set.Data.Add(_line);

            _set.SamplesPerSecond = ParentMetric.Samplerate /* * 2*/;
            _set.Milliseconds = new NodeSystemLib2.TimeStamp((long)(ParentMetric.FFTSize), ParentMetric.Samplerate /* * 2*/).AsSeconds() * 1000;

            _set.AxisX.VisibleMinimum = 0;
            _set.AxisX.VisibleMaximum = ParentMetric.FFTSize;
            _set.AxisY.VisibleMinimum = -0.1;
            _set.AxisY.VisibleMaximum = 1.5;

            _set.AxisX.LabelProvider = x => {
                return $"{(Math.Round(x * (ParentMetric.Samplerate / (double)ParentMetric.FFTSize /*/ 2*/), 1)).ToString(System.Globalization.CultureInfo.InvariantCulture)} Hz";
            };

            _set.Update();
            _plotCtrl.CreateLegend();
        }

        public bool Run {
            get { return run; }

            set {
                run = value;
                timerRefresh.Enabled = run;
            }
        }

        private void DisplayFourierWindow_Load(object sender, EventArgs e) {
            _plotCtrl                = new Plot();
            _plotCtrl.BackColor      = Color.White;
            _plotCtrl.Border         = new Padding(40, 10, 20, 50);
            _plotCtrl.GraphBackColor = Color.LightGray;
            _plotCtrl.Dock           = DockStyle.Fill;
            _plotCtrl.Antialias      = true;
            _plotCtrl.Visible        = true;
            Controls.Add(_plotCtrl);

            _set = new TimeDataSet() { SamplesPerSecond = 1000000 };
            _set.SelectionVisible        = false;
            //_set.AxisX.AbsoluteMinimum = 0;
            _set.AxisX.VisibleMaximum    = 0;
            _set.AxisX.VisibleMaximum    = 100000;
            _plotCtrl.Set                = _set;
            
        }

        private void timerRefresh_Tick(object sender, EventArgs e) {
            PollData();

            if (_redraw) {
                _plotCtrl.Refresh();
                _redraw = false;
            }
        }

        private void PollData() {
            var read = ParentMetric.ReadData(_buffer);
            if (read > 0) {
                _line.Add(_buffer);
                _redraw = true;
            }
        }
    }
}
