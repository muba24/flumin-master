using NodeSystemLib2;
using NodeSystemLib2.Generic;
using NodeSystemLib2.FileFormats;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace MetricRecorder {

    [Metric("Recorder 2", "Recording")]
    public class Recorder2 : StateNode<Recorder2> {

        private const string WorkingDirectory = @"C:\tmp\set0";

        private class WriterObject {
            public object Writer;
            public string Path;
            public TimeStamp FirstWritten { get; private set; }
            public TimeStamp LastWritten { get; private set; }

            private bool FirstWrite = true;

            public void SetNewStamp(TimeStamp stamp) {
                if (FirstWrite) {
                    FirstWrite = false;
                    FirstWritten = stamp;
                }

                LastWritten = stamp;
            }
        }

        private Dictionary<InputPort, WriterObject> _writers = new Dictionary<InputPort, WriterObject>();

        private RecordSet _set;
        private int _samplesBufferMs = 2000;
        private bool _recording;
        private TimeStamp _firstWrittenSampleTime;
        private int _counter;

        private NodeSystemLib2.FormatValue.InputPortValueDouble _portEn;
        private NodeSystemLib2.FormatData1D.InputPortData1D _firstDataIn;
        private NodeSystemLib2.FormatValue.InputPortValueDouble _firstValueIn;

        public string FileMask { get; set; } = "%name% %count%.%ext%";

        public Recorder2(XmlNode node, Graph g) : this(g) {
            Deserializing(node);
        }

        public Recorder2(Graph g) : base("Recorder 2", g) {
            _firstDataIn  = new NodeSystemLib2.FormatData1D.InputPortData1D(this, "DIn 1");
            _firstValueIn = new NodeSystemLib2.FormatValue.InputPortValueDouble(this, "VIn 1");
            _portEn       = new NodeSystemLib2.FormatValue.InputPortValueDouble(this, "En");

            this.PortConnectionChanged += Recorder2_PortConnectionChanged;
        }

        public int SamplesBufferMs {
            get { return _samplesBufferMs; }
            set { _samplesBufferMs = value; }
        }

        public override void PrepareProcessing() {
            foreach (var input in InputPorts.OfType<NodeSystemLib2.FormatData1D.InputPortData1D>().Where(c => c.Connection != null)) {
                var samplesToKeep = (int)((long)input.Samplerate * _samplesBufferMs / 1000);
                input.PrepareProcessing(
                    samplesToKeep,
                    DefaultParameters.DefaultBufferMilliseconds.ToSamples(input.Samplerate)
                );
            }

            foreach (var input in InputPorts.OfType<NodeSystemLib2.FormatValue.InputPortValueDouble>().Where(c => c.Connection != null)) {
                input.PrepareProcessing();
            }
        }

        private void ProcessValues(NodeSystemLib2.FormatValue.InputPortValueDouble port) {
            if (_recording && port != _portEn) {
                NodeSystemLib2.FormatValue.TimeLocatedValue<double> value;

                // copy Count as it could change throughout the loop
                var count = port.Count;

                for (int i = 0; i < count; i++) {
                    if (port.TryDequeue(out value)) {
                        if (_recording) {
                            var writer = _writers[port];
                            if (value.Stamp >= _firstWrittenSampleTime) {
                                ((Stream2DWriter)writer.Writer).WriteSample(value);
                                writer.SetNewStamp(value.Stamp);
                            }
                        }

                    } else {
                        break;
                    }
                }
            }
        }

        private void ProcessStream1D(NodeSystemLib2.FormatData1D.InputPortData1D port) {
            var _buffer           = port.Read();

            WriterObject writer = null;
            Stream1DWriter stream = null;

            if (_recording) {
                writer = _writers[port];
                stream = (Stream1DWriter)writer.Writer;
            }

            foreach (var sample in _buffer.ZipWithValueInput(_portEn)) {
                if (sample.Scalar >= 0.5 && !_recording) {
                    _firstWrittenSampleTime = sample.Stamp;
                    StartRecording();

                    writer = _writers[port];
                    stream = (Stream1DWriter)writer.Writer;

                } else if (sample.Scalar < 0.5 && _recording) {
                    EndRecording();
                }

                if (_recording) {
                    stream.WriteSample(sample.Sample);
                    writer.SetNewStamp(sample.Stamp);
                }
            }
        }

        public override void Process() {
            foreach (var port in InputPorts.OfType<NodeSystemLib2.FormatData1D.InputPortData1D>()) {
                if (port.Connection != null) {
                    ProcessStream1D(port);
                }
            }

            foreach (var port in InputPorts.OfType<NodeSystemLib2.FormatValue.InputPortValueDouble>()) {
                if (port.Connection != null) {
                    ProcessValues(port);
                }
            }
        }

        public override void StopProcessing() {
            if (_recording) {
                EndRecording();
            }
            _firstWrittenSampleTime = TimeStamp.Zero;
            _recording = false;
        }

        private void InitWriters() {
            foreach (var input in InputPorts.OfType<NodeSystemLib2.FormatData1D.InputPortData1D>().Where(c => c.Connection != null)) {
                var path = GetFilename(input, "bin");

                var writer = new Stream1DWriter(
                    new System.IO.BinaryWriter(
                        File.Open(path, FileMode.Create)
                    )
                );

                _writers.Add(input, new WriterObject { Path = path, Writer = writer });
            }

            foreach (var input in InputPorts.OfType<NodeSystemLib2.FormatValue.InputPortValueDouble>().Except(new[] { _portEn }).Where(c => c.Connection != null)) {
                var path = GetFilename(input, "csv");

                var writer = new Stream2DWriter(
                    File.CreateText(path), ','
                );

                _writers.Add(input, new WriterObject { Path = path, Writer = writer });
            }
        }

        private string GetFilename(InputPort port, string ext) {
            var name = string.IsNullOrEmpty(port.Connection?.Parent.Description) ? "signal" : port.Connection.Parent.Description;
            var path = WorkingDirectory;
            var file = FileMask.Replace("%count%", _counter.ToString())
                               .Replace("%name%", name)
                               .Replace("%ext%", ext);

            Directory.CreateDirectory(path);
            var fullPath = Path.Combine(path, file);

            int sameCount = 1;
            while (File.Exists(fullPath)) {
                var newFilename = Path.GetFileNameWithoutExtension(file) + $" ({sameCount++})" + Path.GetExtension(file);
                fullPath = Path.Combine(path, newFilename);
            }

            return fullPath;
        }

        void StartRecording() {
            if (!_recording) {
                System.Diagnostics.Debug.WriteLine("Start recording");
                _set = new RecordSet(Parent); //NodeSystemSettings.Instance.SystemHost.RecordSetForGraph(Parent);
                InitWriters();
                ++_counter;
                _recording = true;
            }
        }

        void EndRecording() {
            if (_recording) {
                System.Diagnostics.Debug.WriteLine("End recording");

                var record = new Record();

                foreach (var input in InputPorts.OfType<NodeSystemLib2.FormatData1D.InputPortData1D>().Where(c => c.Connection != null)) {
                    var writer = _writers[input];

                    var line = new RecordLineStream1D(
                        DateTime.Now,
                        writer.FirstWritten,
                        writer.LastWritten,
                        GetRelativePath(writer.Path, WorkingDirectory),
                        input.Samplerate
                    );

                    record.Lines.Add(line);
                }

                foreach (var input in InputPorts.OfType<NodeSystemLib2.FormatValue.InputPortValueDouble>().Where(c => c.Connection != null && c != _portEn)) {
                    var writer = _writers[input];

                    var line = new RecordLineStream2D(
                        DateTime.Now,
                        writer.FirstWritten,
                        writer.LastWritten,
                        GetRelativePath(writer.Path, WorkingDirectory)
                    );

                    record.Lines.Add(line);
                }

                _set.AddRecord(record);
                RecordSetWriter.WriteToFile(_set, System.IO.Path.Combine(WorkingDirectory, "index.lst"));


                foreach (var writer in _writers.Values.Select(v => v.Writer).OfType<IDisposable>()) {
                    writer.Dispose();
                }
                _writers.Clear();

                _recording = false;
            }
        }

        private static string GetRelativePath(string filespec, string folder) {
            Uri pathUri = new Uri(filespec);
            // Folders must end in a slash
            if (!folder.EndsWith(Path.DirectorySeparatorChar.ToString())) {
                folder += Path.DirectorySeparatorChar;
            }
            Uri folderUri = new Uri(folder);
            return Uri.UnescapeDataString(folderUri.MakeRelativeUri(pathUri).ToString().Replace('/', Path.DirectorySeparatorChar));
        }

        private int EnablePortIndex => InputPorts.TakeWhile(p => p != _portEn).Count();

        public override bool CanProcess => InputPorts.OfType<NodeSystemLib2.FormatData1D.InputPortData1D>().Any(p => p.Available > 0) ||
                                           InputPorts.OfType<NodeSystemLib2.FormatValue.InputPortValueDouble>().Any(p => p.Count > 0);

        public override bool CanTransfer => false;

        private void UpdateInputPorts() {
            var dataInputs  = InputPorts.OfType<NodeSystemLib2.FormatData1D.InputPortData1D>().ToArray();
            var valueInputs = InputPorts.OfType<NodeSystemLib2.FormatValue.InputPortValueDouble>().Except(new [] {_portEn}).ToArray();
            var changed     = true;

            while (changed) {
                changed = false;
                for (int i = 0; i < dataInputs.Length - 1; i++) {
                    if (dataInputs[i].Connection == null && dataInputs[i + 1].Connection != null) {
                        var output = dataInputs[i + 1].Connection;
                        Parent.Disconnect(dataInputs[i + 1].Connection, dataInputs[i + 1]);
                        Parent.Connect(output, dataInputs[i]);
                        changed = true;
                    }
                }
            }

            changed = true;
            while (changed) {
                changed = false;
                for (int i = 0; i < valueInputs.Length - 1; i++) {
                    if (valueInputs[i].Connection == null && valueInputs[i + 1].Connection != null) {
                        var output = valueInputs[i + 1].Connection;
                        Parent.Disconnect(valueInputs[i + 1].Connection, valueInputs[i + 1]);
                        Parent.Connect(output, valueInputs[i]);
                        changed = true;
                    }
                }
            }

            for (int i = InputPorts.Count - 1; i >= 0; i--) {
                if (InputPorts[i].Connection == null) {
                    if (InputPorts[i] != _firstValueIn && InputPorts[i] != _firstDataIn && InputPorts[i] != _portEn) {
                        RemovePort(InputPorts[i]);
                    }
                }
            }
        }

        bool changingConnections = false;
        private void Recorder2_PortConnectionChanged(object sender, ConnectionModifiedEventArgs e) {
            if (changingConnections) return;
            changingConnections = true;

            UpdateInputPorts();

            var dataInputs = InputPorts.OfType<NodeSystemLib2.FormatData1D.InputPortData1D>();
            if (dataInputs.All(d => d.Connection != null)) {
                var p = new NodeSystemLib2.FormatData1D.InputPortData1D(this, $"DIn {dataInputs.Count() + 1}");
                AddPort(p, EnablePortIndex);
            }

            var valueInputs = InputPorts.OfType<NodeSystemLib2.FormatValue.InputPortValueDouble>().Except(new [] {_portEn });
            if (valueInputs.All(d => d.Connection != null)) {
                var p = new NodeSystemLib2.FormatValue.InputPortValueDouble(this, $"VIn {valueInputs.Count() + 1}");
                AddPort(p, EnablePortIndex);
            }

            changingConnections = false;
        }

        public override void StartProcessing() {}
        public override void SuspendProcessing() {}
        public override void Transfer() {}
    }

}
