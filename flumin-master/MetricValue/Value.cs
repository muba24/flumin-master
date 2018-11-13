using System.Globalization;
using System.Xml;
using NodeSystemLib2;
using NodeSystemLib2.Generic;
using NodeSystemLib2.Generic.NodeAttributes;
using NodeSystemLib2.FormatValue;
using System.ComponentModel;
using System;

namespace Flumin.Metrics {

    [Metric("Value", "Values")]
    public class MetricValue : StateNode<MetricValue> {

        private readonly OutputPortValueDouble _portOut;
        private TimeLocatedValue<double> _value;

        private readonly AttributeValueDouble _attrValue;

        public MetricValue(XmlNode node, Graph graph) : this(graph) {
            Deserializing(node);
        }

        public MetricValue(Graph graph) : base("Value", graph) {
            _portOut = new OutputPortValueDouble(this, "Out");
            _attrValue = new AttributeValueDouble(this, "Value");
            TimeValue = new TimeLocatedValue<double>(0, TimeStamp.Zero);

            _attrValue.Changed += attrValue_Changed;
        }

        private void attrValue_Changed(object sender, AttributeChangedEventArgs e) {
            var time = Parent?.GetCurrentClockTime();
            if (time != null) {
                System.Diagnostics.Debug.WriteLine("Value time: " + time.Value);
            }
            TimeValue = new TimeLocatedValue<double>(_attrValue.TypedGet(), time ?? TimeStamp.Zero);
        }

        public TimeLocatedValue<double> TimeValue {
            get { return _value; }
            set {
                _value = value;
                ((OutputPortValueDouble)OutputPorts[0]).BufferForTransfer(value);
            }
        }

        public override bool CanProcess => false;

        public override bool CanTransfer => _portOut.BufferedValueCount > 0;

        public override void StartProcessing() {
            ((OutputPortValueDouble)OutputPorts[0]).BufferForTransfer(new TimeLocatedValue<double>(_attrValue.TypedGet(), TimeStamp.Zero));
        }

        public override void PrepareProcessing() {
            _portOut.PrepareProcessing();
        }

        public override void Transfer() {
            _portOut.TransferBuffer();
        }

        public override void StopProcessing() {}
        public override void SuspendProcessing() {}
        public override void Process() {}
    }
}