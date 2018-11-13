using NodeSystemLib;
using System.Xml;

namespace SimpleADC.Metrics {

    [Metric("Trehshold Event", "Math")]
    class MetricThresholdEvent : NodeSystemLib.StateNode<MetricThresholdEvent> {

        private readonly DataInputPort _dataIn;
        private readonly ValueInputPort _threshIn;
        private readonly EventOutputPort _eventOut;

        private TimeStamp LastEventTime;

        public double CooldownTimeMs { get; set; }

        public MetricThresholdEvent(XmlNode node, Graph g) :this(g) {
            CooldownTimeMs = double.Parse(node.TryGetAttribute("cooldown", "1000"), System.Globalization.CultureInfo.InvariantCulture);
        }

        public MetricThresholdEvent(Graph g) : base("Treshold Event", g) {
            _dataIn   = InputPort.Create<DataInputPort>("Inp", this);
            _threshIn = InputPort.Create<ValueInputPort>("Thresh", this);
            _eventOut = OutputPort.Create<EventOutputPort>("Ev", this);
        }

        protected override void Serializing(XmlWriter writer) {
            writer.WriteAttributeString("cooldown", CooldownTimeMs.ToString(System.Globalization.CultureInfo.InvariantCulture));
            base.Serializing(writer);
        }

        public override bool PrepareProcessing() {
            LastEventTime = TimeStamp.Zero();
            return base.PrepareProcessing();
        }

        protected override void DataAvailable(DataInputPort port) {
            foreach (var sample in _dataIn.Read().ZipWithValueInput(_threshIn)) {
                if (sample.Value >= sample.Scalar) {
                    if ((sample.Stamp - LastEventTime).AsSeconds() * 1000 > CooldownTimeMs) {
                        System.Diagnostics.Debug.WriteLine("event triggered @ " + sample.Stamp);
                        _eventOut.PlanEvent(sample.Stamp);
                        LastEventTime = sample.Stamp;
                    }
                }
            }
        }

    }
}
