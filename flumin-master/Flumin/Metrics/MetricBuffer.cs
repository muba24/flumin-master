using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NodeSystemLib2;
using NodeSystemLib2.Generic;
using NodeSystemLib2.Generic.NodeAttributes;
using NodeSystemLib2.FormatData1D;
using System.Xml;

namespace Flumin.Metrics {

    [Metric("Buffer", "Other")]
    class MetricBuffer : Node {


        // ----------------------------------------------------------------------------------------------------
        // ATTRIBUTES

        private int _samplesToKeep;

        private readonly InputPortData1D  _portInp;
        private readonly OutputPortData1D _portOut;

        private readonly AttributeValueDouble _attrMillis;

        public override bool CanProcess => _portInp.Available > 0;
        public override bool CanTransfer => _portOut.Buffer.Available > 0;


        // ----------------------------------------------------------------------------------------------------
        // CONSTRUCTORS

        public MetricBuffer(XmlNode node, Graph graph) : this(graph) {
            _attrMillis.Set(int.Parse(node.Attributes?.GetNamedItem("ms")?.Value ?? "1000"));
        }

        public MetricBuffer(Graph graph) : base(graph) {
            _portOut = new OutputPortData1D(this, "out");
            _portInp = new InputPortData1D(this, "inp");
            _attrMillis = new AttributeValueDouble(this, "Milliseconds", "ms", 1000);
            _attrMillis.SetRuntimeReadonly();

            _portInp.SamplerateChanged += (sender, ev) => _portOut.Samplerate = _portInp.Samplerate;
        }


        // ----------------------------------------------------------------------------------------------------
        // PUBLIC METRIC SETTINGS
        public override string ToString() => Name;


        // ----------------------------------------------------------------------------------------------------
        // METRIC ACTIONS

        public override void PrepareProcessing() {
            if (_portInp.Samplerate <= 0) {
                Parent.Context.Notify(new GraphNotification(GraphNotification.NotificationType.Error, "Input samplerate must be > 0 Hz"));
            }
            InitBuffer();
        }

        public override void Transfer() {
            _portOut.Transfer();
        }

        public override void Process() {
            if (_portInp.Available >= _samplesToKeep) {
                var buffer = _portInp.Read(_portOut.Buffer.Free);
                _portOut.Buffer.Write(buffer, 0, buffer.Available);
            }
        }

        protected override void Serializing(XmlWriter writer) {
            writer.WriteAttributeString("ms", _attrMillis.Serialize());
        }


        // ----------------------------------------------------------------------------------------------------
        // HELPERS

        private void InitBuffer() {
            _samplesToKeep = (int)(_portInp.Samplerate * _attrMillis.TypedGet() / 1000);
            _portInp.PrepareProcessing(2 * _samplesToKeep, 2 * _samplesToKeep);
            _portOut.PrepareProcessing(2 * _samplesToKeep, 2 * _samplesToKeep);
        }

        public override void StartProcessing() {}
        public override void StopProcessing() {}
        public override void SuspendProcessing() {}


    }
}
