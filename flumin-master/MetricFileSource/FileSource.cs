using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using NodeSystemLib;
using NodeSystemLib.FileFormatLib;
using System.IO;
using System.ComponentModel;
using System.Xml;
using System.Drawing;

namespace MetricFileSource {

    [Metric("File", "Unknown")]
    public class FileNode : Node {

        private class StateEntry : Tuple<long, Dictionary<Node, NodeState>> {
            public StateEntry(long position, Dictionary<Node, NodeState> states) : base(position, states) { }
            public long Position => Item1;
            public Dictionary<Node, NodeState> States => Item2;
        }

        private readonly DataOutputPort _portOut;

        private List<bool>          _servedLookup;
        private int                 _samplerate;
        private Stream1DReader      _reader;
        private Thread              _thread;
        private TimeLocatedBuffer   _buffer;
        private volatile bool       _running;
        private FileNodeWindow      _seekWindow;
        private long                _fileTargetPosition;

        private List<StateEntry> _stateBag;

        public FileNode(XmlNode node, Graph g) : this(g) {
            FileName = TryGetAttribute(node, "file", otherwise: "");
            Samplerate = int.Parse(TryGetAttribute(node, "samplerate", otherwise: "1000000"));
        }

        protected override void Serializing(XmlWriter writer) {
            writer.WriteAttributeString("file", FileName);
            writer.WriteAttributeString("samplerate", Samplerate.ToString());
        }

        public FileNode(Graph g)
        : base("File", g, InputPort.CreateMany(), OutputPort.CreateMany(OutputPort.Create("Out", PortDataType.Array))) {
            _portOut = (DataOutputPort)OutputPorts[0];
        }

        [EditorAttribute(typeof(System.Windows.Forms.Design.FileNameEditor), typeof(System.Drawing.Design.UITypeEditor))]
        public string FileName { get; set; }

        public int Samplerate {
            get {
                return _samplerate;
            }
            set {
                _samplerate = value;
                _portOut.Samplerate = _samplerate;
            }
        }

        [Browsable(false)]
        public bool Seeking => _thread?.IsAlive ?? false;

        public override bool PrepareProcessing() {
            if (!File.Exists(FileName)) {
                NodeSystemSettings.Instance.SystemHost.ReportError(this, $"File not found: {FileName}");
                return false;
            }

            try {
                _reader = new Stream1DReader(
                    new BinaryReader(
                        File.Open(FileName, FileMode.Open, FileAccess.Read, FileShare.Read)
                    )
                );
            } catch (Exception e) {
                NodeSystemSettings.Instance.SystemHost.ReportError(this, $"Could not open {FileName}. Unknown exception: {e}");
                return false;
            }

            if (_buffer == null || _buffer.Samplerate != Samplerate) {
                _buffer = new TimeLocatedBuffer(NodeSystemSettings.Instance.SystemHost.GetDefaultBufferSize(Samplerate), Samplerate);
            }

            _stateBag = new List<StateEntry>();

            _buffer.ResetTime();
            _fileTargetPosition = 0;

            if (NodeSystemSettings.Instance.SystemHost.DockPanelInstance != null) {
                _seekWindow                 = new FileNodeWindow();
                _seekWindow.Max             = _reader.SampleCount;
                _seekWindow.Min             = 0;
                _seekWindow.Value           = 0;
                _seekWindow.ValueBackground = 0;
            }

            _servedLookup = new List<bool>(Enumerable.Range(0, _portOut.Connections.Count).Select(i => false));

            return true;
        }

        [Browsable(false)]
        public long TargetPosition {
            get { return _fileTargetPosition; }
            set {
                _fileTargetPosition = value;
                RunPositionThread();
            }
        }

        [Browsable(false)]
        public long CurrentPosition {
            get { return _reader.Position; }
        }

        protected override void ProcessingStarted() {
            _running = true;
            //RunPositionThread();

            var states = Parent.SaveState();
            _stateBag.Add(new StateEntry(_reader.Position, states));

            if (NodeSystemSettings.Instance.SystemHost.DockPanelInstance != null) {
                _seekWindow.Show((WeifenLuo.WinFormsUI.Docking.DockPanel)NodeSystemSettings.Instance.SystemHost.DockPanelInstance, WeifenLuo.WinFormsUI.Docking.DockState.DockBottom);
                _seekWindow.CloseButtonVisible = false;
                _seekWindow.ValueChanged += seekWindow_ValueChanged;
            }
        }

        private void seekWindow_ValueChanged(object sender, FileNodeWindow.ValueChangedEventArgs e) {
            _fileTargetPosition = e.Value;
            RunPositionThread();
        }

        private void RunPositionThread() {
            StopPositionThread();
            _thread = new Thread(DataThread);
            _thread.Start();
        }

        private void StopPositionThread() {
            if (_thread != null) {
                if (!_running) {
                    _thread.Join();
                } else {
                    _running = false;
                    _thread.Join();
                    _running = true;
                }
            }
        }

        private StateEntry GetNearestState(long position) {
            var nearestIndex = -1;
            var nearestValue = _reader.SampleCount;

            for (int i = _stateBag.Count - 1; i >= 0; i--) {
                var diff = Math.Abs(position - _stateBag[i].Position);
                if (_stateBag[i].Position < position && diff < nearestValue) {
                    nearestIndex = i;
                    nearestValue = diff;
                }
            }

            if (nearestIndex > -1) {
                return _stateBag[nearestIndex];
            }

            return null;
        }

        private void DataThread() {
            const int N_States  = 10;
            var blockTotal      = _reader.SampleCount / _buffer.Length;
            var stateSaveFactor = blockTotal / N_States;
            var totalRead       = 0L;

            // find last state from bag
            var nearestState = GetNearestState(_fileTargetPosition);
            if (nearestState != null) {
                var nearestStateDistance = _fileTargetPosition - nearestState.Position;
                var currentDistance      = _fileTargetPosition - _reader.Position;

                if (currentDistance < 0 || currentDistance > nearestStateDistance) {
                    Parent.LoadState(nearestState.States);

                    System.Diagnostics.Debug.WriteLine("Load state from position " + nearestState.Position);

                    if (_seekWindow != null) {
                        _seekWindow.ValueBackground = _reader.Position;
                    }
                }
            }

            var newStateRefPos = _reader.Position;
            if (_stateBag.Count > 0 && _stateBag.Last().Position < newStateRefPos) {
                newStateRefPos = _stateBag.Last().Position;
            }

            // current block in file 
            totalRead = _reader.Position;

            while (_running) {
                if (_fileTargetPosition > _reader.Position) {
                    // save state
                    //if (totalRead % (stateSaveFactor * _buffer.Length) == 0) {
                    if ((_reader.Position - newStateRefPos) > (stateSaveFactor * _buffer.Length)) { 
                        if (!_stateBag.Exists(t => t.Item1 == _reader.Position)) {
                            _seekWindow?.AddStatePointMarker(_reader.Position);
                            var states = Parent.SaveState();
                            _stateBag.Add(new StateEntry(_reader.Position, states));
                            newStateRefPos = _reader.Position;
                            System.Diagnostics.Debug.WriteLine("Added to statebag: " + _reader.Position);
                        }
                    }

                    // push data into graph
                    var continueReading = true;
                    try {
                        var shouldRead = Math.Min(_buffer.Length, _fileTargetPosition - _reader.Position);
                        totalRead     += FillBuffer(shouldRead);
                    } catch (IOException) {
                        continueReading = false;
                    }

                    DistributeBuffer();

                    if (!continueReading) {
                        NodeSystemSettings.Instance.SystemHost.StopProcessing();
                        return;
                    }

                } else {
                    break;
                }
            }
        }

        private void DistributeBuffer() {
            var servedCount = 0;
            while (servedCount < _servedLookup.Count && _running) {
                for (int i = 0; i < _servedLookup.Count; i++) {
                    if (!_servedLookup[i]) {
                        var remoteBuffer = ((DataInputPort)_portOut.Connections[i]).Queue;
                        if (remoteBuffer.Capacity - remoteBuffer.Length >= _buffer.WrittenSamples) {
                            ((DataInputPort)_portOut.Connections[i]).RecieveData(_buffer);
                            _servedLookup[i] = true;
                            servedCount++;
                        }
                    }
                }
                Thread.Sleep(1);
            }
            for (int i = 0; i < _servedLookup.Count; i++) _servedLookup[i] = false;
        }

        private long FillBuffer(long samples) {
            var arr  = _buffer.GetSamples();
            var res  = true;
            var read = 0L;

            read = _reader.ReadSamples(arr, 0, samples);

            if (_seekWindow != null) {
                _seekWindow.ValueBackground = _reader.Position;
            }

            _buffer.SetWritten((int)read);

            return read;
        }

        protected override void ProcessingStopped() {
            _running = false;
            _thread?.Join();

            _seekWindow?.Close();
            _seekWindow?.Dispose();
            _reader.Dispose();
            _reader = null;
            _seekWindow = null;
        }

        public override NodeState SaveState() {
            var state = NodeState.Save(this, new TimeStamp(CurrentPosition, Samplerate));
            if (CurrentPosition % 2 != 0) {
                System.Diagnostics.Debug.WriteLine("oh");
            }
            state["position"] = CurrentPosition;
            state["time"] = _buffer.CurrentTime;
            return state;
        }

        public override void LoadState(NodeState state) {
            state.Load();
            _buffer.SetTime((TimeStamp)state["time"]);
            _reader.Seek((long)state["position"], SeekOrigin.Begin);
        }

        private static string TryGetAttribute(XmlNode node, string name, string otherwise) {
            return node?.Attributes?.GetNamedItem(name)?.Value ?? otherwise;
        }

        public override string ToString() => Name;
        
    }

}
