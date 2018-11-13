using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using WeifenLuo.WinFormsUI.Docking;
using NewOpenGLRenderer;
using NodeSystemLib2;
using NodeSystemLib2.Generic;
using NodeSystemLib2.FormatData1D;
using NodeSystemLib2.FormatValue;
using static MetricTimeDisplay.MetricDisplay2;

namespace MetricTimeDisplay {

    public partial class Display2Window : DockContent {

        const bool DEBUG_VISIBLE = false;

        private TimeDataSet _set;
        private bool        _redraw;

        private readonly Dictionary<InputPort, ITimeData> _renderers = new Dictionary<InputPort, ITimeData>();

        private float[] _float;
        private bool run;
    
        public Display2Window() {
            InitializeComponent();
            this.Disposed += Display2Window_Disposed;
            HideOnClose = true;

            if (!DEBUG_VISIBLE) {
                tableLayoutPanel1.RowStyles[0].Height = 100;
                tableLayoutPanel1.RowStyles[1].Height = 0;
            }
        }

        private void Display2Window_Disposed(object sender, EventArgs e) {
            System.Diagnostics.Debug.WriteLine("DISPLAY WINDOW DISPOSED");
        }

        public double Samplerate { get; set; }

        [Browsable(false)]
        public MetricDisplay2 ParentMetric { get; set; }

        [Browsable(false)]
        public Dictionary<InputPort, IChannel> Channels => ParentMetric.Channels;

        public bool Run {
            get { return run; }

            set {
                run = value;
                timerRefresh.Enabled = run;
            }
        }

        public bool IsLoaded { get; private set; }

        public void PrepareProcessing() {
            InitRenderers();
            _set.Milliseconds = ParentMetric.Milliseconds;

            var dataChannels = Channels.Values.OfType<DataChannel>();
            if (dataChannels.Any()) {
                _float = new float[dataChannels.Max(d => d.DisplayBuffer.Capacity)];
            }
        }

        public void StopProcessing() {
            _set?.Clear();
        }

        private void PollData() {
            foreach (var port in _renderers.Keys.OfType<InputPortData1D>()) {
                // ReadFromDataPort should read whole buffer
                // and overwrite whole Line Buffer
                var channel = (MetricDisplay2.DataChannel)Channels[port];
                var lastStamp = channel.DisplayBuffer.Time;
                var read = ParentMetric.ReadFromDataPort(port, channel.DisplayBuffer);
                if (read > 0 && lastStamp < channel.DisplayBuffer.Time) {
                    AddData1D(port, channel.DisplayBuffer);
                    _redraw = true;
                }
            }

            foreach (var port in _renderers.Keys.OfType<InputPortValueDouble>()) {
                var channel = (MetricDisplay2.ValueChannel)Channels[port];
                while (channel.Data.Count > 0) {
                    var v = channel.Data.Dequeue();
                    AddData2D(port, v);
                }
            }
        }
        
        private void AddData2D(InputPortValueDouble p, TimeLocatedValue<double> v) {
            if (IsDisposed) return;
            lock (_renderers) {
                var render = ((DataLine2D)_renderers[p]);
                render.Add(new PointF(v.Stamp.ToRate(render.SamplesPerSecond), (float)v.Value));
            }
        }

        private void AddData1D(InputPort p, TimeLocatedBuffer1D<double> buffer) {
            if (IsDisposed) {
                System.Diagnostics.Debug.WriteLine("AddData1D: Disposed");
                return;
            }

            lock (_renderers) {
                ((DataLine1D)_renderers[p]).Add(buffer);
            }
        }

        public void ClearChannels() {
            _renderers.Clear();
        }

        public bool ChannelExists(InputPortData1D p) {
            return _renderers.ContainsKey(p);
        }

        private void InitRenderers() {
            _set.Data.Clear();
            _renderers.Clear();
            foreach (var port in Channels.Keys) {
                if (port is InputPortData1D) {
                    AddRenderer((InputPortData1D)port, ((MetricDisplay2.DataChannel)Channels[port]).DisplayBuffer.Capacity);
                } else if (port is InputPortValueDouble) {
                    AddRenderer((InputPortValueDouble)port, ParentMetric.Milliseconds);
                }
            }
            _set.Update();
            _plotCtrl.CreateLegend();

            if (DEBUG_VISIBLE) {
                CreateInfoPanel();
            }
        }

        private void CreateInfoPanel() {
            flowLayoutPanel.Controls.Clear();
            foreach (var renderer in _renderers.Values.OfType<DataLine1D>()) {
                var ctrl = new LineInfo();
                ctrl.Line = (DataLine1D)renderer;
                ctrl.Visible = true;
                flowLayoutPanel.Controls.Add(ctrl);
            }
        }

        private void UpdateInfoPanel() {
            foreach (var ctrl in flowLayoutPanel.Controls.OfType<LineInfo>()) {
                ctrl.UpdateLabels();
            }
        }

        private void AddRenderer(InputPortValueDouble p, double millisWindow) {
            var colors = new [] { Color.Red, Color.Blue, Color.Green, Color.Orange, Color.Navy, Color.Black };
            var data = new DataLine2D(p.Connection.Parent.Description, millisWindow) {
                LineColor = colors[_renderers.Count % colors.Length],
                SamplesPerSecond = 1000000
            };
            _renderers.Add(p, data);
            _set?.Data.Add(data);
        }

        private void AddRenderer(InputPortData1D p, int samples) {
            var colors = new [] { Color.Red, Color.Blue, Color.Green, Color.Orange, Color.Navy, Color.Black };
            var data = new DataLine1D(p.Connection.Parent.Description, samples, p.Samplerate) {
                LineColor = colors[_renderers.Count % colors.Length]
            };
            _renderers.Add(p, data);
            _set?.Data.Add(data);
        }

        private void Display2Window_Load(object sender, EventArgs e) {
            InitSet();
            IsLoaded = true;
        }

        private void InitSet() {
            _set = new TimeDataSet() { SamplesPerSecond = 1000000 };
            _set.SelectionVisible = false;
            //_set.AxisX.AbsoluteMinimum = 0;
            _set.AxisX.VisibleMaximum = 0;
            _set.AxisX.VisibleMaximum = 100000;
            _plotCtrl.Set = _set;
        }

        public void SetXAxis(TimeStamp min, TimeStamp max) {
            _set.AxisX.VisibleMinimum = min.ToRate(_set.SamplesPerSecond);
            _set.AxisX.VisibleMaximum = max.ToRate(_set.SamplesPerSecond);
        }

        private void timerRefresh_Tick(object sender, EventArgs e) {
            PollData();

            if (DEBUG_VISIBLE) {
                UpdateInfoPanel();
            }

            if (_redraw) {
                _plotCtrl.Refresh();
                _redraw = false;
            }
        }

        private void Display2Window_Shown(object sender, EventArgs e) {
            _plotCtrl.UpdateSizes();
        }

    }
}
