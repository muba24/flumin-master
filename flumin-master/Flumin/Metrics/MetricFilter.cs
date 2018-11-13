using System;
using System.ComponentModel;
using System.Globalization;
using System.Xml;
using NodeSystemLib;

namespace SimpleADC.Metrics {

    [Metric("Convolution Filter", "Math")]
    class MetricFilter : Node {

        private readonly Biquad _bpf = new Biquad(Biquad.BiquadType.Bandpass, 1, 1, 0);
        private double _fc;

        DataInputPort _portInp;
        DataOutputPort _portOut;

        public MetricFilter(XmlNode node, Graph graph) : this(graph) {
            Fc              = double.Parse(node.Attributes?.GetNamedItem("fc").Value    ?? "1000");
            _bpf.Q          = double.Parse(node.Attributes?.GetNamedItem("q").Value     ?? "1");
            _bpf.PeakGainDb = double.Parse(node.Attributes?.GetNamedItem("gain").Value  ?? "0");
            var type        = node.Attributes?.GetNamedItem("filtertype").Value         ?? "Bandpass";
            _bpf.Type       = (Biquad.BiquadType) Enum.Parse(typeof (Biquad.BiquadType), type);
        }

        public MetricFilter(Graph graph)
            : base("Filter",graph,
                InputPort.CreateMany(InputPort.Create("in", PortDataType.Array)),
                OutputPort.CreateMany(OutputPort.Create("out", PortDataType.Array))) {

            _portInp = (DataInputPort)InputPorts[0];
            _portOut = (DataOutputPort)OutputPorts[0];

            Fc = 100000;
        }

        protected override void Serializing(XmlWriter writer) {
            writer.WriteAttributeString("fc", _fc.ToString(CultureInfo.InvariantCulture));
            writer.WriteAttributeString("q", _bpf.Q.ToString(CultureInfo.InvariantCulture));
            writer.WriteAttributeString("gain", _bpf.PeakGainDb.ToString(CultureInfo.InvariantCulture));
            writer.WriteAttributeString("filtertype", _bpf.Type.ToString());
        }

        public Biquad.BiquadType FilterType {
            get { return _bpf.Type; }
            set { _bpf.Type = value; }
        }

        [Description("Eck- bzw. Bandmittenfrequenz")]
        public double Fc {
            get { return _fc; }
            set {
                _fc = value;
                if (_portOut.Samplerate > 0) {
                    _bpf.Fc = _fc/ _portOut.Samplerate;
                }
            }
        }

        [Description("Bandbreite")]
        public double Q {
            get { return _bpf.Q; }
            set { _bpf.Q = value; }
        }

        [Description("Peak/Shelf Gain")]
        public double Gain {
            get { return _bpf.PeakGainDb; }
            set { _bpf.PeakGainDb = value; }
        }

        private TimeLocatedBuffer _inputBuffer;
        private TimeLocatedBuffer _outputBuffer;

        protected override void ProcessingStarted() {
            //
        }

        protected override void InputSamplerateChanged(InputPort e) {
            _inputBuffer  = new TimeLocatedBuffer(GlobalSettings.Instance.BufferSize(_portInp.Samplerate), _portInp.Samplerate);
            _outputBuffer = new TimeLocatedBuffer(GlobalSettings.Instance.BufferSize(_portInp.Samplerate), _portInp.Samplerate);

            ((DataInputPort)InputPorts[0]).InitBuffer();

            _portOut.Samplerate = _portInp.Samplerate;
            _bpf.Fc = Fc/ _portInp.Samplerate;
        }

        protected override void DataAvailable(DataInputPort port) {
            while (((DataInputPort) InputPorts[0]).Queue.Length > _inputBuffer.Length) {
                Process();
            }
        }

        public void Process() {
            ((DataInputPort) InputPorts[0]).Queue.Dequeue(_inputBuffer);

            var bufIn = _inputBuffer.GetSamples();
            var bufOut = _outputBuffer.GetSamples();

            for (var i = 0; i < _inputBuffer.Length; i++) {
                bufOut[i] = _bpf.Process(bufIn[i]);
            }

            _outputBuffer.SetWritten(_inputBuffer.Length);
            ((DataOutputPort) OutputPorts[0]).SendData(_outputBuffer);
        }

        public override string ToString() => Name;
    }
}