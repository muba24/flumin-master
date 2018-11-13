using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NodeSystemLib;
using System.ComponentModel;
using System.IO;
using System.Globalization;
using System.Xml;

namespace MetricRecorder {

    [Metric("Value Recorder", "Recording")]
    class MetricValueRecorder : StateNode<MetricValueRecorder> {

        private readonly ValueInputPort _portInp;
        private readonly ValueInputPort _portEn;

        private StreamWriter _writer = null;
        private FileStream _file     = null;
        private bool _recording      = false;

        public MetricValueRecorder(XmlNode node, Graph graph) : this(graph) { }

        public MetricValueRecorder(Graph graph) : base("Value Recorder", graph) {

            _portInp = InputPort.Create<ValueInputPort>("In", this);
            _portEn = InputPort.Create<ValueInputPort>("En", this);
        }

        public string FileMask { get; set; } = "values %count%.csv";

        public override string ToString() => Name;

        [Browsable(false)]
        public int Counter { get; private set; }

        protected override void ProcessingStopped() {
            Recording = false;
        }

        private void StartRecording() {
            _file = File.OpenWrite(GetFilename());
            _writer = new StreamWriter(_file);
            WriteHeader();
        }

        private void StopRecording() {
            _writer.Flush();
            _writer.Close();
            _file.Dispose();
            _writer = null;
            _file = null;
            Counter++;
        }

        private void WriteHeader() {
            _writer.WriteLine("TIME,VALUE");
        }

        private void ProcessValue(TimeLocatedValue value) {
            var date = value.Stamp.ToShortTimeString() ?? "null";
            _writer?.WriteLine($"{date},{value.Value.ToString(CultureInfo.InvariantCulture)}");
        }

        [Browsable(false)]
        public bool Recording {
            get {
                return _recording;
            }
            set {
                if (value != _recording) {
                    _recording = value;
                    if (_recording) StartRecording();
                    else StopRecording();
                }
            }
        }

        protected override void ValueAvailable(ValueInputPort port) {
            TimeLocatedValue value = null;

            while (_portEn.Values.Count > 0) {
                if (_portEn.Values.TryDequeue(out value)) {
                    Recording = value.Value > 0.5;
                }
            }

            while (_portInp.Values.Count > 0) {
                if (_portInp.Values.TryDequeue(out value)) {
                    ProcessValue(value);
                }
            }
        }

        private string GetFilename() {
            var path = Parent.WorkingDirectory;
            var file = FileMask.Replace("%count%", Counter.ToString());
            Directory.CreateDirectory(path);
            return Path.Combine(path, file);
        }

    }

}
