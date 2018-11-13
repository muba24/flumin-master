using System.Xml;
using NodeSystemLib;

namespace SimpleADC.Metrics {

    [Metric("Downsample", "Math")]
    class MetricHalfrate : Node {

        private TimeLocatedBuffer _inputBuffer;
        private TimeLocatedBuffer _outputBuffer;

        DataInputPort _portInp;
        DataOutputPort _portOut;

        public MetricHalfrate(XmlNode node, Graph graph) : this(graph) { }

        public MetricHalfrate(Graph graph)
            : base("Half Rate",graph,
                InputPort.CreateMany(InputPort.Create("in", PortDataType.Array)),
                OutputPort.CreateMany(OutputPort.Create("out", PortDataType.Array))) {

            _portInp = (DataInputPort)InputPorts[0];
            _portOut = (DataOutputPort)OutputPorts[0];
        }

        protected override void InputSamplerateChanged(InputPort e) {
            _portOut.Samplerate = _portInp.Samplerate / 2;
            ((DataInputPort)InputPorts[0]).InitBuffer();
            _inputBuffer = new TimeLocatedBuffer(GlobalSettings.Instance.BufferSize(_portInp.Samplerate), _portInp.Samplerate);
            _outputBuffer = new TimeLocatedBuffer(GlobalSettings.Instance.BufferSize(_portOut.Samplerate), _portOut.Samplerate);
        }

        public override string ToString() => Name;

        protected override void DataAvailable(DataInputPort port) {
            while (((DataInputPort)InputPorts[0]).Queue.Length > _inputBuffer.Length) {
                Process();
            }
        }

        public void Process() {
            ((DataInputPort)InputPorts[0]).Queue.Dequeue(_inputBuffer);

            var smpIn = _inputBuffer.GetSamples();
            var smpOut = _outputBuffer.GetSamples();

            for (var i = 0; i < _inputBuffer.WrittenSamples / 2; i++) {
                smpOut[i] = smpIn[i * 2];
            }

            _outputBuffer.SetWritten(_inputBuffer.WrittenSamples / 2);

            ((DataOutputPort)OutputPorts[0]).SendData(_outputBuffer);
        }

        public override NodeState SaveState() {
            return NodeState.Save(this, Parent.GetCurrentClockTime());
        }

        public override void LoadState(NodeState state) {
            System.Diagnostics.Debug.Assert(state.Parent == this);
            state.Load();
        }

    }
}