using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using NewOpenGLRenderer;
using WeifenLuo.WinFormsUI.Docking;

namespace MetricTimeDisplay {

    public partial class DisplayWaterfall : DockContent {

        WaterfallPlot _plot;
        private bool run;
        private NodeSystemLib2.FormatDataFFT.TimeLocatedBufferFFT _buffer;

        public DisplayWaterfall() {
            InitializeComponent();
        }

        private void DisplayWaterfall_Load(object sender, EventArgs e) {
            _plot = new WaterfallPlot();
            _plot.MouseMove += _plot_MouseMove;
            _plot.Dock = DockStyle.Fill;
            Controls.Add(_plot);
        }

        private void _plot_MouseMove(object sender, MouseEventArgs e) {
            if (!Run) {
                _plot.Invalidate();
            }
        }

        [Browsable(false)]
        public TimeDisplayWaterfall ParentMetric { get; set; }

        [Browsable(false)]
        public float DbMin {
            get { return _plot.DbMin; }
            set { _plot.DbMin = value; }
        }

        [Browsable(false)]
        public float DbMax {
            get { return _plot.DbMax; }
            set { _plot.DbMax = value; }
        }

        public void PrepareProcessing(int fftCount) {
            _plot.Init(ParentMetric.FFTSize, fftCount, ParentMetric.Samplerate);
            _buffer = new NodeSystemLib2.FormatDataFFT.TimeLocatedBufferFFT(1, ParentMetric.FFTSize, ParentMetric.Samplerate);
        }

        public bool Run {
            get { return run; }

            set {
                run = value;
                timerRefresh.Enabled = run;
            }
        }

        private void timerRefresh_Tick(object sender, EventArgs e) {
            PollData();
            _plot.Invalidate();
        }

        private void PollData() {
            var avail = ParentMetric.Available;

            for (int i = 0; i < avail; i++) {
                var read = ParentMetric.ReadData(_buffer);
                if (read == 0) break;
                _plot.AddFrame(_buffer.Data);
            }
        }

    }

}
