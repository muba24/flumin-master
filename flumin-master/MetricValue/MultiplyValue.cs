using NodeSystemLib2;
using NodeSystemLib2.Generic;
using NodeSystemLib2.FormatData1D;
using NodeSystemLib2.FormatValue;
using System.Diagnostics;
using System.Xml;
using System;

namespace MetricValue {

    [Metric("Multiply", "Math")]
    public class MetricMultiplyValue : StateNode<MetricMultiplyValue> {

        private readonly InputPortData1D  _portInp;
        private readonly InputPortValueDouble _portInpVal;
        private readonly OutputPortData1D _portOut;

        private TimeLocatedBuffer1D<double> _bufOut;

        public override bool CanProcess => _portInp.Available > 0;
        public override bool CanTransfer => _portOut.Buffer.Available > 0;

        public MetricMultiplyValue(XmlNode node, Graph graph) : this(graph) {
            Deserializing(node);
        }

        public override string ToString() => Name;

        public MetricMultiplyValue(Graph graph) : base("Multiply", graph) {
            _portInp = new InputPortData1D(this, "In");
            _portInpVal = new InputPortValueDouble(this, "f");
            _portOut = new OutputPortData1D(this, "out");

            _portInp.SamplerateChanged += (s, e) => _portOut.Samplerate = _portInp.Samplerate;
        }

        public override void PrepareProcessing() {
            _portInp.PrepareProcessing();
            _portOut.PrepareProcessing();

            _bufOut = new TimeLocatedBuffer1D<double>(
                DefaultParameters.DefaultBufferMilliseconds.ToSamples(_portInp.Samplerate), _portInp.Samplerate
            );

            _portInpVal.PrepareProcessing();
        }

        public override void Process() {
            var bufIn = _portInp.Read(_bufOut.Capacity);

            var count = 0;
            var output = _bufOut.Data;
            foreach (var sample in bufIn.ZipWithValueInput(_portInpVal)) {
                output[count++] = sample.Sample * sample.Scalar;
            }

            _bufOut.SetWritten(count);
            _portOut.Buffer.Write(_bufOut.Data, 0, _bufOut.Available);
        }

        public override void StartProcessing() {}
        public override void StopProcessing() {}
        public override void SuspendProcessing() {}

        public override void Transfer() {
            _portOut.Transfer();
        }
    }
}
