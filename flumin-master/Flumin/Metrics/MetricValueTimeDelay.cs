using NodeSystemLib2;
using NodeSystemLib2.Generic;
using NodeSystemLib2.FormatData1D;
using NodeSystemLib2.FormatValue;
using NodeSystemLib2.Generic.NodeAttributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Flumin.Metrics {

    [Metric("Value Time Delay", "Value")]
    class MetricValueTimeDelay : StateNode<MetricValueTimeDelay> {
        private InputPortValueDouble _portIn;
        private OutputPortValueDouble _portOut;

        private AttributeValueDouble _attrMillisDelay;

        public MetricValueTimeDelay(XmlNode n, Graph g) : this(g) {
            _attrMillisDelay.Deserialize(n.TryGetAttribute(_attrMillisDelay.Name, "0"));
        }

        public MetricValueTimeDelay(Graph g) : base("Value Time Delay", g) {
            _portIn = new InputPortValueDouble(this, "In");
            _portOut = new OutputPortValueDouble(this, "Out");
            _attrMillisDelay = new AttributeValueDouble(this, "Delay", "ms", 0);
        }

        protected override void Serializing(XmlWriter writer) {
            writer.WriteAttributeString(_attrMillisDelay.Name, _attrMillisDelay.Serialize());
            base.Serializing(writer);
        }

        public override bool CanProcess => _portIn.Count > 0;
        public override bool CanTransfer => _portOut.BufferedValueCount > 0;

        public override void PrepareProcessing() {
            _portIn.PrepareProcessing();
            _portOut.PrepareProcessing();
        }

        public override void Process() {
            var count = _portIn.Count;

            for (int i = 0; i < count; i++) {
                TimeLocatedValue<double> value;
                if (_portIn.TryDequeue(out value)) {
                    _portOut.BufferForTransfer(new TimeLocatedValue<double>(value.Value, value.Stamp.Increment(_attrMillisDelay.TypedGet() / 1000.0)));
                } else {
                    break;
                }
            }
        }

        public override void StartProcessing() {}

        public override void StopProcessing() {}

        public override void SuspendProcessing() {}

        public override void Transfer() {
            _portOut.TransferBuffer();
        }

    }

}
