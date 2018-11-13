using NodeSystemLib2;
using NodeSystemLib2.Generic;
using NodeSystemLib2.FormatDataFFT;
using System;
using System.ComponentModel;
using System.Drawing;
using System.Xml;

namespace MetricTimeDisplay {

    [Metric("Fourier Time Display", "Display")]
    public class TimeFourierDisplay : StateNode<TimeFourierDisplay>, INodeUi {

        private readonly InputPortDataFFT _port;
        private DisplayFourierWindow _wnd;

        private RingBufferFFT _queue;

        private object _portLock = new object();
        private object _wndLock  = new object();

        public TimeFourierDisplay(XmlNode node, Graph g) : this(g) {
            Deserializing(node);
        }

        public TimeFourierDisplay(Graph g) : base("Time Fourier Display", g) {
            _port = new InputPortDataFFT(this, "In");
        }

        public override void PrepareProcessing() {
            if (_port.FFTSize <= 0) throw new InvalidOperationException("FFT Size must be > 0!");

            _port.PrepareProcessing(
                Math.Max(DefaultParameters.MinimumQueueFrameCount, DefaultParameters.DefaultQueueMilliseconds.ToFrames(_port.Samplerate, _port.FrameSize)),
                Math.Max(DefaultParameters.MinimumBufferFrameCount, DefaultParameters.DefaultBufferMilliseconds.ToFrames(_port.Samplerate, _port.FrameSize))
            );

            _queue = new RingBufferFFT(1, FFTSize, Samplerate) {
                Overflow = true
            };

            lock (_wndLock) {
                if (_wnd != null) {
                    _wnd.PrepareProcessing();
                    _wnd.Run = true;
                }
            }
        }

        public override void StopProcessing() {
            lock (_wndLock) {
                if (_wnd != null) _wnd.Run = false;
            }
        }

        [Browsable(false)]
        public int FFTSize => _port.FFTSize;

        [Browsable(false)]
        public int Samplerate => _port.Samplerate;

        public override bool CanProcess => _port.Available > 0;
        public override bool CanTransfer => false;

        public int ReadData(TimeLocatedBufferFFT output) {
            lock (_portLock) {
                var read = _queue.Peek(output, output.Capacity);
                return read;
            }
        }

        public override void Process() {
            lock (_portLock) {
                var frames = _port.Read();
                _queue.Write(frames, 0, 1);
            }
        }

        public void OnLoad(NodeEditorLib.EditorControl.Node node) {
            //
        }

        public void OnDoubleClick() {
            lock (_wndLock) {
                if (_wnd == null || _wnd.IsDisposed) {
                    _wnd = new DisplayFourierWindow();
                    _wnd.ParentMetric = this;
                    _wnd.Show(
                        (WeifenLuo.WinFormsUI.Docking.DockPanel)Parent.Context.DockPanel,
                        WeifenLuo.WinFormsUI.Docking.DockState.DockRight
                    );

                    if (State == Graph.State.Running) {
                        _wnd.PrepareProcessing();
                        _wnd.Run = true;
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
