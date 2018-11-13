using System.Linq;
using NodeSystemLib2;
using System.Xml;
using System;

namespace MetricAccumulator {

    [Metric(nameof(Accumulator), "Math")]
    public class Accumulator : Node {

        private readonly NodeSystemLib2.FormatData1D.InputPortData1D _portIn;

        public Accumulator(XmlNode node, Graph g) : this(g) {}

        public Accumulator(Graph g) : base(g) {
            Name = "Accumulator";
            _portIn = new NodeSystemLib2.FormatData1D.InputPortData1D(this, "In");
        }

        public override void PrepareProcessing() {
            _portIn.PrepareProcessing(
                DefaultParameters.DefaultQueueMilliseconds.ToSamples(_portIn.Samplerate),
                DefaultParameters.DefaultBufferMilliseconds.ToSamples(_portIn.Samplerate)
            );
        }

        public override bool CanProcess => _portIn.Available > 0;
        public override bool CanTransfer => false;

        public override void StartProcessing() {}
        public override void StopProcessing() {}
        public override void SuspendProcessing() {}
        public override void Transfer() { }

        public override void Process() {
            var buffer = _portIn.Read();
            var sum = buffer.Data.Take(buffer.Available).Sum();
            TotalSum += sum;
        }

        public double TotalSum { get; set; }

    }
}
