using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NodeSystemLib2;
using NodeSystemLib2.Generic;
using NodeSystemLib2.Generic.NodeAttributes;
using NodeSystemLib2.FormatDataFFT;
using System.Xml;
using System.ComponentModel;
using System.Drawing;

namespace MetricTimeDisplay {

    [Metric("FFT Waterfall", "Display")]
    public class TimeDisplayWaterfall : StateNode<TimeDisplayWaterfall>, INodeUi {

        private readonly InputPortDataFFT _port;
        private DisplayWaterfall _wnd;
        private RingBufferFFT _queue;

        private readonly AttributeValueDouble _attrDbMin;
        private readonly AttributeValueDouble _attrDbMax;
        private readonly AttributeValueInt _attrTimeWindow;

        private readonly object _portLock = new object();
        private readonly object _wndLock  = new object();

        public TimeDisplayWaterfall(XmlNode node, Graph g) : this(g) {
            Deserializing(node);
        }

        public TimeDisplayWaterfall(Graph g) : base("Waterfall", g) {
            _port = new InputPortDataFFT(this, "In");

            _attrDbMax = new AttributeValueDouble(this, "dB max", "dB");
            _attrDbMin = new AttributeValueDouble(this, "dB min", "dB");
            _attrTimeWindow = new AttributeValueInt(this, "Window Length", "ms");

            _attrDbMin.Changed += (s, e) => {
                if (_attrDbMin.TypedGet() >= _attrDbMax.TypedGet()) _attrDbMin.Set(_attrDbMax.TypedGet() - 0.0001);
                if (_wnd != null) _wnd.DbMin = (float)_attrDbMin.TypedGet();
            };

            _attrDbMax.Changed += (s, e) => {
                if (_attrDbMax.TypedGet() <= _attrDbMin.TypedGet()) _attrDbMax.Set(_attrDbMin.TypedGet() + 0.0001);
                if (_wnd != null) _wnd.DbMax = (float)_attrDbMax.TypedGet();
            };

            _attrTimeWindow.Changed += (s, e) => {
                UpdateWindowSettings();
            };
            _attrTimeWindow.SetRuntimeReadonly();
        }

        public override string ToString() => Name;

        private void UpdateWindowSettings() {
            var frameDuration = _port.FFTSize * 1000 / (double)Samplerate;
            var frameCount = _attrTimeWindow.TypedGet() / frameDuration;

            lock (_wndLock) {
                if (_wnd != null && !_wnd.IsDisposed) {
                    _wnd.PrepareProcessing((int)frameCount);
                    _wnd.DbMin = (float)_attrDbMin.TypedGet();
                    _wnd.DbMax = (float)_attrDbMax.TypedGet();
                    _wnd.Run = true;
                }
            }
        }

        public override void PrepareProcessing() {
            if (_port.FFTSize <= 0) {
                throw new InvalidOperationException("FFT Size must be > 0!");
            }

            _port.PrepareProcessing(
                Math.Max(DefaultParameters.MinimumQueueFrameCount, DefaultParameters.DefaultQueueMilliseconds.ToFrames(_port.Samplerate, _port.FrameSize)),
                Math.Max(DefaultParameters.MinimumBufferFrameCount, DefaultParameters.DefaultBufferMilliseconds.ToFrames(_port.Samplerate, _port.FrameSize))
            );

            _queue = new RingBufferFFT(_port.Capacity, FFTSize, _port.Samplerate) {
                Overflow = true
            };

            UpdateWindowSettings();
        }

        public override void StopProcessing() {
            lock (_wndLock) {
                if (_wnd != null) _wnd.Run = false;
            }
        }

        public int FFTSize => _port.FFTSize;
        public int Samplerate => _port.Samplerate;
        public int Available => _queue.Available;

        public override bool CanProcess => _port.Available > 0;
        public override bool CanTransfer => false;

        public int ReadData(TimeLocatedBufferFFT output) {
            lock (_portLock) {
                var read = _queue.Read(output, output.Capacity);
                return read;
            }
        }

        public override void Process() {
            lock (_portLock) {
                var frames = _port.Read();
                if (frames.Available == 0) return;
                _queue.Write(frames, 0, frames.Available);
            }
        }

        public void OnLoad(NodeEditorLib.EditorControl.Node node) {
            //
        }

        public void OnDoubleClick() {
            lock (_wndLock) {
                if (_wnd == null || _wnd.IsDisposed) {
                    _wnd = new DisplayWaterfall();
                    _wnd.ParentMetric = this;
                    _wnd.Show(
                        (WeifenLuo.WinFormsUI.Docking.DockPanel)Parent.Context.DockPanel,
                        WeifenLuo.WinFormsUI.Docking.DockState.DockRight
                    );

                    if (State == Graph.State.Running) {
                        UpdateWindowSettings();
                    }
                }
            }

        }

        public void OnDraw(Rectangle node, Graphics e) {
            //
        }

        public override void SuspendProcessing() {}
        public override void StartProcessing() {}
        public override void Transfer() {}
    }
}
