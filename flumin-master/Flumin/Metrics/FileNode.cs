using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using NodeSystemLib;
using System.IO;
using System.ComponentModel;
using System.Xml;
using NodeGraphControl;
using System.Drawing;

namespace SimpleADC.Metrics {

    [Metric("File", "Unknown")]
    public class FileNode : Node, INodeUi {

        private class StateEntry : Tuple<long, Dictionary<Node, NodeState>> {
            public StateEntry(long position, Dictionary<Node, NodeState> states) : base(position, states) {}
            public long Position => Item1;
            public Dictionary<Node, NodeState> States => Item2;
        }

        private readonly DataOutputPort _portOut;

        private List<bool>          _servedLookup;
        private int                 _samplerate;
        private FileStream          _fileStream;
        private BinaryReader        _reader;
        private Thread              _thread;
        private TimeLocatedBuffer   _buffer;
        private volatile bool       _running;
        private FileNodeWindow      _seekWindow;
        private long                _fileTargetPosition;

        private List<StateEntry> _stateBag;

        public FileNode(XmlNode node, Graph g) : this(g) {
            FileName   = node.TryGetAttribute("file", otherwise: "");
            Samplerate = int.Parse(node.TryGetAttribute("samplerate", otherwise:  "1000000"));
        }

        protected override void Serializing(XmlWriter writer) {
            writer.WriteAttributeString("file", FileName);
            writer.WriteAttributeString("samplerate", Samplerate.ToString());
        }

        public FileNode(Graph g)
        : base("File", g, InputPort.CreateMany(), OutputPort.CreateMany(OutputPort.Create("Out", PortDataType.Array)))
        {
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
                GlobalSettings.Instance.Errors.Add(new Error($"File not found: {FileName}"));
                return false;
            }

            try {
                _fileStream = File.OpenRead(FileName);
            } catch (Exception e) {
                GlobalSettings.Instance.Errors.Add(new Error($"Could not open {FileName}. Unknown exception: {e}"));
                return false;
            }

            _reader = new BinaryReader(_fileStream);

            if (_buffer == null || _buffer.Samplerate != Samplerate) {
                _buffer = new TimeLocatedBuffer(GlobalSettings.Instance.BufferSize(Samplerate), Samplerate);
            }

            _stateBag = new List<StateEntry>();

            _buffer.ResetTime();
            _fileTargetPosition = 0;

            if (GlobalSettings.Instance.DockPanelInstance != null) {
                _seekWindow = new FileNodeWindow();
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
            get { return _fileStream.Position; }
        }

        protected override void ProcessingStarted() {
            _running = true;
            RunPositionThread();

            if (GlobalSettings.Instance.DockPanelInstance != null) {
                _seekWindow.Show(GlobalSettings.Instance.DockPanelInstance, WeifenLuo.WinFormsUI.Docking.DockState.DockBottom);
                _seekWindow.CloseButtonVisible = false;
                _seekWindow.PercentChanged += _seekWindow_PercentChanged;
            }
        }

        private void _seekWindow_PercentChanged(object sender, double e) {
            _fileTargetPosition = (long)(e * _fileStream.Length);
            _fileTargetPosition = _fileTargetPosition - (_fileTargetPosition % sizeof(double));
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
            var nearestValue = _fileStream.Length;

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
            var blockTotal      = _fileStream.Length / sizeof(double) / _buffer.Length;
            var stateSaveFactor = blockTotal / N_States;

            // find last state from bag
            var nearestState = GetNearestState(_fileTargetPosition);
            if (nearestState != null) {
                var nearestStateDistance = _fileTargetPosition - nearestState.Position;
                var currentDistance      = _fileTargetPosition - _fileStream.Position;

                if (currentDistance < 0 || currentDistance > nearestStateDistance) {
                    Parent.LoadState(nearestState.States);

                    System.Diagnostics.Debug.WriteLine("Load state from position " + nearestState.Position);

                    if (_seekWindow != null) {
                        _seekWindow.PercentDone = _fileStream.Position / (double)_fileStream.Length;
                    }
                }
            }
                                            
            // current block in file 
            var blockCounter = _fileStream.Position / sizeof(double) / _buffer.Length;

            while (_running) {
                if (_fileTargetPosition > _fileStream.Position) {
                    // save state
                    if (blockCounter % stateSaveFactor == 0) {
                        if (!_stateBag.Exists(t => t.Item1 == _fileStream.Position)) {
                            if (_seekWindow != null) {
                                _seekWindow.AddSavedStateTimePercent(_fileStream.Position / (double)_fileStream.Length);
                            }
                            var states = Parent.SaveState();
                            _stateBag.Add(new StateEntry(_fileStream.Position, states));
                        }
                    }

                    // push data into graph
                    var continueReading = FillBuffer(Math.Min(_buffer.Length, (_fileTargetPosition - _fileStream.Position) / sizeof(double)));
                    DistributeBuffer();
                    ++blockCounter;

                    if (!continueReading) {
                        GlobalSettings.Instance.StopProcessing();
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

        private bool FillBuffer(long samples) {
            var arr = _buffer.GetSamples();
            var res = true;
            var i   = 0;

            try {
                for (; i < samples; i++) {
                    arr[i] = _reader.ReadDouble();
                }
            } catch (EndOfStreamException) {
                res = false;
            }

            if (_seekWindow != null) {
                _seekWindow.PercentDone = _fileStream.Position / (double)_fileStream.Length;
            }

            _buffer.SetWritten(i);

            return res;
        }

        protected override void ProcessingStopped() {
            _running = false;
            _thread.Join();

            _seekWindow?.Close();
            _seekWindow?.Dispose();
            _reader.Dispose();
            _fileStream.Dispose();
            _reader       = null;
            _fileStream   = null;
            _seekWindow   = null;
        }

        public override NodeState SaveState() {
            var state = NodeState.Save(this, new TimeStamp(CurrentPosition / sizeof(double), Samplerate));
            state["position"] = CurrentPosition;
            state["time"] = _buffer.CurrentTime;
            return state;
        }

        public override void LoadState(NodeState state) {
            state.Load();
            _buffer.SetTime((TimeStamp)state["time"]);
            _fileStream.Seek((long)state["position"], SeekOrigin.Begin);
        }


        public override string ToString() => Name;

        public void OnLoad(NodeGraphNode node) {
            //
        }

        public void OnDoubleClick() {
            //
        }

        public void OnDraw(Rectangle node, Graphics e) {
            //
        }
    }

}
