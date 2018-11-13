//using SimpleADC.NodeSystem;
using NodeSystemLib;
using System.Linq;

namespace SimpleADC.Metrics {

    [Metric("Accumulator", "Math")]
    class MetricAccumulate : StateNode<MetricAccumulate> {

        public MetricAccumulate(Graph g) : base("Accumulate", g) {
            InputPort.Create<DataInputPort>("inp", this);
        }

        [State]
        public double Accumulator { get; set; }

        protected override void DataAvailable(DataInputPort port) {
            Accumulator += port.Read().GetSamples()
                                      .Take(port.Buffer.WrittenSamples)
                                      .Sum();
        }

        public override string ToString() => Name;

    }
}
