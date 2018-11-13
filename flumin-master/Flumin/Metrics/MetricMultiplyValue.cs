using NodeSystemLib;
using System.Diagnostics;
using System.Xml;


namespace SimpleADC.Metrics {

    [Metric("Multiply", "Math")]
    class MetricMultiplyValue : Node {

        private DataInputPort  _portInp;
        private ValueInputPort _inputValue;
        private DataOutputPort _portOut;

        private TimeLocatedBuffer _bufIn;
        private TimeLocatedBuffer _bufOut;

        private Stopwatch _sw;
        private int longestLoopTime;
        private int longestBufSize;

        public double Multiplier { get; set; } = 1;

        public MetricMultiplyValue(XmlNode node, Graph graph) : this(graph) {
        }

        public override string ToString() => Name;

        public MetricMultiplyValue(Graph graph)
            : base("Multiply", graph,
                InputPort.CreateMany(InputPort.Create("in", PortDataType.Array), 
                                     InputPort.Create("f", PortDataType.Value)),
                OutputPort.CreateMany(OutputPort.Create("out", PortDataType.Array))) {

            _portInp    = (DataInputPort)InputPorts[0];
            _inputValue = (ValueInputPort)InputPorts[1];
            _portOut    = (DataOutputPort)OutputPorts[0];
        }

        protected override void InputSamplerateChanged(InputPort e) {
            _portInp.InitBuffer();
            _bufIn  = new TimeLocatedBuffer(GlobalSettings.Instance.BufferSize(_portInp.Samplerate), _portInp.Samplerate);
            _bufOut = new TimeLocatedBuffer(GlobalSettings.Instance.BufferSize(_portInp.Samplerate), _portInp.Samplerate);
            _portOut.Samplerate = _portInp.Samplerate;
        }

        protected override void DataAvailable(DataInputPort port) {
            //while (_portInp.Buffer.Length > _bufIn.Length) {
                Process();
            //}
        }

        private void Process() {
            _portInp.Queue.Dequeue(_bufIn);

            var samples = _bufIn.GetSamples();
            var output  = _bufOut.GetSamples();

            // 1: Get Stamp for current sample
            // 2: Get Multiplier Value from Input
            // 3: Multiply
            // 4: Remove unnecessary values from Value List

            var currentSampleTime = new TimeLocatedValue(0, new TimeStamp(0));
            TimeLocatedValue fLast = new TimeLocatedValue(1, new TimeStamp(0));

            var stamp = _bufIn.FrontTime;
            var sr = _bufIn.Samplerate;

            for (int i = 0; i < _bufIn.Length; i++) {
                stamp.AddInplace(1, sr);
                currentSampleTime.SetStamp(stamp);

                // TODO: Values should cache current and next value
                TimeLocatedValue multiplier;
                if (_inputValue.Values.SafeTryWeakPredecessor(currentSampleTime, out multiplier)) {
                    output[i] = samples[i] * multiplier.Value;
                    if (multiplier != fLast) {
                        fLast = multiplier;
                        _inputValue.Values.SafeRemoveRangeTo(multiplier);
                    }
                } else {
                    output[i] = samples[i] * fLast.Value;
                }
            }

            _bufOut.SetWritten(_bufIn.Length);
            _portOut.SendData(_bufOut);
        }

    }

}
