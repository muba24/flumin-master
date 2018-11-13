using NodeGraphControl;
using NodeSystemLib;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace MetricRecorder
{
    //[Metric("Time Recorder", "Recording")]
    public class MetricRecorder : StateNode<MetricRecorder>, INodeUi {

        private NodeSystemLib.FileFormatLib.RecordSet _set;

        private TimeLocatedBuffer               _buffer;
        private BinaryWriter                    _binWriter;
        private FileStream                      _stream;
        private int                             _counter;
        private int                             _samplesBufferMs = 2000;
        private int                             _samplesToKeep;
        private bool                            _recording;
        private readonly DataInputPort          _portData;
        private readonly ValueInputPort         _portEnable;
        private TimeStamp                       _firstWrittenSampleTime;
        private long                            _writtenSamples;

        public string FileMask { get; set; } = "signal %count%.bin";

        public MetricRecorder(XmlNode node, Graph g): this(g) { }

        public MetricRecorder(Graph g) : base("Recorder", g) {
            _portData = InputPort.Create<DataInputPort>("Data", this);
            _portEnable = InputPort.Create<ValueInputPort>("En", this);

            _set = NodeSystemSettings.Instance.SystemHost.RecordSetForGraph(Parent);
        }

        public int SamplesBufferMs {
            get { return _samplesBufferMs; }
            set {
                _samplesBufferMs = value;
                _samplesToKeep = (int)((long)_portData.Samplerate * SamplesBufferMs / 1000);
            }
        }

        public override string ToString() => Name;


        public override bool PrepareProcessing() {
            if (_portData.Queue == null || _portData.Queue.Samplerate != _portData.Samplerate || _portData.Queue.Length < _samplesToKeep) {
                _portData.InitBuffer(_samplesToKeep * 2);
            } else {
                _portData.Queue.Clear();
            }

            if (_buffer == null || _buffer.Samplerate != _portData.Samplerate) {
                _buffer = new TimeLocatedBuffer(NodeSystemSettings.Instance.SystemHost.GetDefaultBufferSize(_portData.Samplerate), _portData.Samplerate);
            }

            return true;
        }

        protected override void InputSamplerateChanged(InputPort e) {
            _samplesToKeep = (int)((long)_portData.Samplerate * SamplesBufferMs / 1000);
        }

        protected override void ProcessingStopped() {
            lock (this) {
                // Flush rest of buffer if need be.
                // This has to be synchronized since a Stop could be triggered
                // even though the node is still processing data.
                Process();
            }

            if (_stream != null) {
                EndRecording();
            }

            _portData.Queue.Clear();
            _buffer.ResetTime();
        }

        protected override void DataAvailable(DataInputPort port) {
            lock (this) {
                while (_portData.Queue.Length > _buffer.Length) {
                    Process();
                }
            }
        }

        private void Process() {
            _portData.Queue.Dequeue(_buffer);
            Analyze();
        }

        private void Analyze() {
            var currentSampleTime = new TimeLocatedValue(0, new TimeStamp(0));

            var arr = _buffer.GetSamples();

            for (int i = 0; i < _buffer.WrittenSamples; i++) {
                var stamp = _buffer.StampForSample(i);
                currentSampleTime.SetStamp(stamp);

                TimeLocatedValue value;
                if (_portEnable.Values.SafeTryWeakPredecessor(currentSampleTime, out value)) {
                    if (value.Value >= 0.5 && !_recording) {
                        _firstWrittenSampleTime = value.Stamp;
                        StartRecording();
                    } else if (value.Value < 0.5 && _recording) {
                        EndRecording();
                    }
                }

                if (_recording) {
                    _binWriter.Write(arr[i]);
                    _writtenSamples++;
                }
            }
        }

        private void StartRecording() {
            _stream = File.Open(GetFilename(), FileMode.Create);
            _binWriter = new BinaryWriter(_stream);
            ++_counter;
            _recording = true;
        }

        private void EndRecording() {
            if (_stream != null) {
                var record = new NodeSystemLib.FileFormatLib.Record();

                var line = new NodeSystemLib.FileFormatLib.RecordLineStream1D(
                    DateTime.Now, 
                    _firstWrittenSampleTime,  
                    _firstWrittenSampleTime.Add(_writtenSamples, _portData.Samplerate),
                    GetRelativePath(_stream.Name, Parent.WorkingDirectory), 
                    _portData.Samplerate
                );

                record.Lines.Add(line);

                _set.AddRecord(record);
                NodeSystemLib.FileFormatLib.RecordSetWriter.WriteToFile(_set, System.IO.Path.Combine(Parent.WorkingDirectory, "index.lst"));
            }

            _firstWrittenSampleTime = TimeStamp.Zero();
            _writtenSamples = 0;
            _recording = false;

            _stream?.Close();
            _stream?.Dispose();
            _binWriter = null;
            _stream = null;
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

        private string GetFilename() {
            var path = Parent.WorkingDirectory;
            var file = FileMask.Replace("%count%", _counter.ToString());
            Directory.CreateDirectory(path);
            var fullPath = Path.Combine(path, file);

            int sameCount = 1;
            while (File.Exists(fullPath)) {
                var newFilename = Path.GetFileNameWithoutExtension(file) + $" ({sameCount++})" + Path.GetExtension(file);
                fullPath = Path.Combine(path, newFilename);
            }

            return fullPath;
        }

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
