using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NodeSystemLib2;
using NodeSystemLib2.Generic;
using NodeSystemLib2.Generic.NodeAttributes;
using NodeSystemLib2.FileFormats;
using System.Xml;

namespace MetricRecorder
{
    [Metric("Recorder 3", "Recording")]
    public class Recorder3 : StateNode<Recorder3> {

        private class RecorderLineValue : RecorderLine {

            private Stream2DWriter _writer;
            private string _path;
            private object _writerLock = new object();

            public TimeStamp LastTimeStampCheck { get; set; }

            public new NodeSystemLib2.FormatValue.InputPortValueDouble Port => (NodeSystemLib2.FormatValue.InputPortValueDouble)base.Port;

            public RecorderLineValue(NodeSystemLib2.FormatValue.InputPortValueDouble port, Recorder3 parent) : base(port, parent) { }

            public void Write(NodeSystemLib2.FormatValue.TimeLocatedValue<double> sample) {
                if (_writer == null) StartRecording(sample.Stamp);
                _writer.WriteSample(sample);
            }

            public RecordLineStream2D GetRecordLine() {
                return new RecordLineStream2D(
                    DateTime.Now, Start, End, _path
                );
            }

            protected override void OnRecordingStarted() {
                lock (_writerLock) {
                    _path = CreateOutputFilename("values", "csv");
                    var handle = System.IO.File.CreateText(_path);
                    _writer = new Stream2DWriter(handle, ',');
                }
            }

            protected override void OnRecordingStopped() {
                lock (_writer) {
                    _writer.Dispose();
                    Parent.Parent.Context.AddRecordedFileToSet(GetRecordLine());
                    LastTimeStampCheck = TimeStamp.Zero;
                }
            }
        }

        private class RecorderLine1D : RecorderLine {

            private System.IO.BinaryWriter _binaryStream;
            private Stream1DWriter _writer;
            private string _path;
            private object _writerLock = new object();

            public new NodeSystemLib2.FormatData1D.InputPortData1D Port => (NodeSystemLib2.FormatData1D.InputPortData1D)base.Port;

            public RecorderLine1D(NodeSystemLib2.FormatData1D.InputPortData1D port, Recorder3 parent) : base(port, parent) {}

            public RecordLineStream1D GetRecordLine() {
                return new RecordLineStream1D(
                    DateTime.Now, Start, End, _path, Port.Samplerate
                );
            }

            public void Write(double sample) {
                _writer.WriteSample(sample);
            }

            protected override void OnRecordingStarted() {
                lock (_writerLock) {
                    _path = CreateOutputFilename("signal", "bin");
                    var handle = System.IO.File.Open(_path, System.IO.FileMode.Create, System.IO.FileAccess.Write);
                    _binaryStream = new System.IO.BinaryWriter(handle);
                    _writer = new Stream1DWriter(_binaryStream);
                }
            }

            protected override void OnRecordingStopped() {
                lock (_writer) {
                    _writer.Dispose();
                    _binaryStream.Dispose();
                    Parent.Parent.Context.AddRecordedFileToSet(GetRecordLine());
                }
            }
        }

        private abstract class RecorderLine {
            public InputPort Port { get; }
            public Recorder3 Parent { get; }

            public TimeStamp Start { get; private set; }
            public TimeStamp End { get; private set; }

            private volatile bool _recording;

            public bool Recording => _recording;

            public void StartRecording(TimeStamp time) {
                if (!_recording) {
                    _recording = true;
                    Start = time;
                    OnRecordingStarted();
                }
            }
            protected abstract void OnRecordingStarted();

            public void StopRecording(TimeStamp time) {
                if (_recording) {
                    _recording = false;
                    End = time;
                    OnRecordingStopped();
                }
            }
            protected abstract void OnRecordingStopped();

            protected string CreateOutputFilename(string prefix, string extension) {
                var ctx = Parent.Parent.Context;
                var mask = System.IO.Path.Combine(ctx.WorkingDirectory, ctx.FileMask);

                var fullpath = mask.Replace("%date%", DateTime.Now.ToShortDateString())
                                   .Replace("%name%", prefix)
                                   .Replace("%ext%", extension);

                var filename = System.IO.Path.GetFileName(fullpath);
                var path = System.IO.Path.GetDirectoryName(fullpath);
                System.IO.Directory.CreateDirectory(path);

                int sameCount = 1;
                while (System.IO.File.Exists(fullpath)) {
                    var newFilename = System.IO.Path.GetFileNameWithoutExtension(filename) + $" ({sameCount++})" + System.IO.Path.GetExtension(filename);
                    fullpath = System.IO.Path.Combine(path, newFilename);
                }

                return fullpath;
            }

            public RecorderLine(InputPort port, Recorder3 parent) {
                Port = port;
                Parent = parent;
                _recording = false;
            }

        }

        private Dictionary<PortDataType, string> _portTypePrefix = new Dictionary<PortDataType, string>() {
            { PortDataTypes.TypeIdFFT, "FFT" },
            { PortDataTypes.TypeIdSignal1D, "Signal" },
            { PortDataTypes.TypeIdValueDouble, "Value" }
        };

        private List<RecorderLine> _lines = new List<RecorderLine>();
        private NodeSystemLib2.FormatValue.InputPortValueDouble _portEnable;

        private AttributeValueDouble _attrPrebufferLengthMs;
        private AttributeValueInt _attrNumOfLines1D;
        private AttributeValueInt _attrNumOfLines2D;

        public Recorder3(XmlNode node, Graph g) : this(g) {
            //Deserializing(node);
            _attrNumOfLines1D.Deserialize(node.Attributes[_attrNumOfLines1D.Name]?.Value ?? "1");
            _attrNumOfLines2D.Deserialize(node.Attributes[_attrNumOfLines2D.Name]?.Value ?? "1");
            _attrPrebufferLengthMs.Deserialize(node.Attributes[_attrPrebufferLengthMs.Name]?.Value ?? "1000");
        }

        public Recorder3(Graph g) : base("Recorder 3", g) {
            CreateLine(PortDataTypes.TypeIdSignal1D);
            CreateLine(PortDataTypes.TypeIdValueDouble);
            _portEnable = new NodeSystemLib2.FormatValue.InputPortValueDouble(this, "Enable");
            _attrPrebufferLengthMs = new AttributeValueDouble(this, "PreBuffer", "ms", 1000);

            _attrNumOfLines1D = new AttributeValueInt(this, "NumOfLines1D", 1);
            _attrNumOfLines2D = new AttributeValueInt(this, "NumOfLines2D", 1);
            _attrNumOfLines1D.SetRuntimeReadonly();
            _attrNumOfLines2D.SetRuntimeReadonly();
            _attrNumOfLines1D.Changed += _numOfLines1D_Changed;
            _attrNumOfLines2D.Changed += _numOfLines2D_Changed;
        }

        protected override void Serializing(XmlWriter writer) {
            writer.WriteAttributeString(_attrNumOfLines1D.Name, _attrNumOfLines1D.Serialize());
            writer.WriteAttributeString(_attrNumOfLines2D.Name, _attrNumOfLines2D.Serialize());
            writer.WriteAttributeString(_attrPrebufferLengthMs.Name, _attrPrebufferLengthMs.Serialize());
        }

        private void BuildDataLineView() {
            var data1dConnections = _lines.Where(line => line.Port.DataType == PortDataTypes.TypeIdSignal1D).ToArray();
            var data2dConnections = _lines.Where(line => line.Port.DataType == PortDataTypes.TypeIdValueDouble).ToArray();

            if (_portEnable.Connection != null) {
                Parent.Disconnect(_portEnable.Connection, _portEnable);
            }
            RemovePort(_portEnable);

            foreach (var line in data1dConnections) {
                if (line.Port.Connection != null) {
                    Parent.Disconnect(line.Port.Connection, line.Port);
                }
                RemovePort(line.Port);
            }

            foreach (var line in data2dConnections) {
                if (line.Port.Connection != null) {
                    Parent.Disconnect(line.Port.Connection, line.Port);
                }
                RemovePort(line.Port);
            }

            foreach (var line in data1dConnections) AddPort(line.Port);
            foreach (var line in data2dConnections) AddPort(line.Port);
            AddPort(_portEnable);
        }

        private void _numOfLines2D_Changed(object sender, AttributeChangedEventArgs e) {
            var countShouldBe = _attrNumOfLines2D.TypedGet();
            var countIs = _lines.Count(line => line.Port.DataType == PortDataTypes.TypeIdValueDouble);

            if (countIs < countShouldBe) {
                for (int i = countIs; i < countShouldBe; i++) {
                    CreateLine(PortDataTypes.TypeIdValueDouble);
                }
            } else if (countIs > countShouldBe) {
                for (int i = countShouldBe; i < countIs; i++) {
                    RemoveLine(_lines.Last(line => line.Port.DataType == PortDataTypes.TypeIdValueDouble));
                }
            }

            BuildDataLineView();
        }

        private void _numOfLines1D_Changed(object sender, AttributeChangedEventArgs e) {
            var countShouldBe = _attrNumOfLines1D.TypedGet();
            var countIs = _lines.Count(line => line.Port.DataType == PortDataTypes.TypeIdSignal1D);

            if (countIs < countShouldBe) {
                for (int i = countIs; i < countShouldBe; i++) {
                    CreateLine(PortDataTypes.TypeIdSignal1D);
                }
            } else if (countIs > countShouldBe) {
                for (int i = countShouldBe; i < countIs; i++) {
                    RemoveLine(_lines.Last(line => line.Port.DataType == PortDataTypes.TypeIdSignal1D));
                }
            }

            BuildDataLineView();
        }

        private void RemoveLine(RecorderLine line) {
            if (line.Port.Connection != null) {
                var graph = Parent;
                graph.Disconnect(line.Port.Connection, line.Port);
            }
            RemovePort(line.Port);
            _lines.Remove(line);
        }

        private void CreateLine(PortDataType type) {
            Func<int> PortCount = () => _lines.Count(l => l.Port.DataType.Equals(type));
            
            RecorderLine line = null;
            if (type == PortDataTypes.TypeIdValueDouble) {
                var port = new NodeSystemLib2.FormatValue.InputPortValueDouble(this, $"{_portTypePrefix[type]}{PortCount()}");
                line = new RecorderLineValue(port, this);
            } else if (type == PortDataTypes.TypeIdSignal1D) {
                var port = new NodeSystemLib2.FormatData1D.InputPortData1D(this, $"{_portTypePrefix[type]}{PortCount()}");
                line = new RecorderLine1D(port, this);
            } else {
                throw new ArgumentException(nameof(type));
            }
            line.Port.ConnectionChanged += LineStateChanged;
            _lines.Add(line);
        }

        private void LineStateChanged(object sender, ConnectionModifiedEventArgs e) {
        }

        public override bool CanProcess {
            get {
                bool hasData = false;

                foreach (var line in _lines) {
                    if (line.Port is NodeSystemLib2.FormatData1D.InputPortData1D) {
                        var port = (NodeSystemLib2.FormatData1D.InputPortData1D)line.Port;
                        hasData |= port.Available > 0;
                    } else if (line.Port is NodeSystemLib2.FormatValue.InputPortValueDouble) {
                        var port = (NodeSystemLib2.FormatValue.InputPortValueDouble)line.Port;
                        hasData |= port.Count > 0;
                    }
                }

                return hasData;
            }
        }

        public override bool CanTransfer => false;

        public override void Process() {
            foreach (var line in _lines) {
                if (line.Port.Connection != null) {
                    if (line is RecorderLine1D) {
                        var l = (RecorderLine1D)line;
                        var count = l.Port.Available * 1000 / (double)l.Port.Samplerate - _attrPrebufferLengthMs.TypedGet();
                        if (count > 0) {
                            ProcessSignal1D(l, (int)(count / 1000.0 * l.Port.Samplerate));
                        }
                    } else if (line is RecorderLineValue) {
                        var l = (RecorderLineValue)line;
                        ProcessValues(l);
                    }
                }
            }
        }

        private void ProcessSignal1D(RecorderLine1D line, int samples) {
            var buffer = line.Port.Read(samples);

            foreach (var tuple in buffer.ZipWithValueInput(_portEnable)) {
                if (line.Recording) {
                    if (tuple.Scalar < 0.5) {
                        line.StopRecording(tuple.Stamp);
                    } else {
                        line.Write(tuple.Sample);
                    }
                } else if (tuple.Scalar >= 0.5) {
                    line.StartRecording(tuple.Stamp);
                    line.Write(tuple.Sample);
                }
            }
        }

        private void ProcessValues(RecorderLineValue line) {
            var port = line.Port;

            // copy count as it might grow while doing the loop
            var cnt = port.Count;

            for (int i = 0; i < cnt; i++) {
                NodeSystemLib2.FormatValue.TimeLocatedValue<double> v;
                if (port.TryDequeue(out v)) {
                    if (v.Stamp > Parent.GetCurrentClockTime()) {
                        // not time yet for this value, put back
                        port.Write(v);
                    } else {
                        foreach (var en in _portEnable.GetValueIterator(line.LastTimeStampCheck)) {
                            if (en.Value < 0.5) line.StopRecording(en.Stamp);
                            else line.StartRecording(en.Stamp);

                            if (en.Stamp > v.Stamp) {
                                // even if there are more values in portEnable, not relevant for value v
                                break;
                            }
                        }
                        line.LastTimeStampCheck = v.Stamp;
                        if (line.Recording) line.Write(v);
                    }
                }
            }
        }

        public override void PrepareProcessing() {
            foreach (var line in _lines) {
                if (line.Port.Connection != null) {
                    if (line is RecorderLine1D) {
                        var l = (RecorderLine1D)line;
                        l.Port.PrepareProcessing(
                            (int)(2 * _attrPrebufferLengthMs.TypedGet() * l.Port.Samplerate / 1000),
                            (int)(2 * _attrPrebufferLengthMs.TypedGet() * l.Port.Samplerate / 1000)
                        );
                    } else if (line is RecorderLineValue) {
                        var l = (RecorderLineValue)line;
                        l.Port.PrepareProcessing();
                    } else {
                        throw new NotImplementedException();
                    }
                }
            }
            _portEnable.PrepareProcessing();
        }

        public override void StartProcessing() { }

        public override FlushState FlushData() {
            var didProcess = false;
            foreach (var line in _lines.Where(l => l.Recording)) {
                if (line is RecorderLine1D && ((RecorderLine1D)line).Port.Available > 0) {
                    ProcessSignal1D((RecorderLine1D)line, ((RecorderLine1D)line).Port.Available);
                    didProcess = true;
                } else if (line is RecorderLineValue && ((RecorderLineValue)line).Port.Count > 0) {
                    ProcessValues((RecorderLineValue)line);
                    didProcess = true;
                }
            }

            if (didProcess) {
                Process();
                return FlushState.Some;
            }
            return FlushState.Empty;
        }

        public override void StopProcessing() {
            foreach (var line in _lines) {
                if (line.Recording) {
                    line.StopRecording(Parent.GetCurrentClockTime());
                }
            }
        }

        public override void SuspendProcessing() { }

        public override void Transfer() { }
    }
}
