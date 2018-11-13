using System.Globalization;
using System.Xml;
using NodeSystemLib;
using System.ComponentModel;
using NodeGraphControl;
using System;
using System.Drawing;

namespace SimpleADC.Metrics {

    [Metric("Value", "Values")]
    public class MetricValue : Node {

        private TimeLocatedValue _value;

        public MetricValue(XmlNode node, Graph graph) : this(graph) {
            Value = double.Parse(node.TryGetAttribute("value", "0"));
        }

        public MetricValue(Graph graph)
            : base("Value", graph,
                InputPort.CreateMany(), 
                OutputPort.CreateMany(OutputPort.Create("out", PortDataType.Value)))
        {
            TimeValue = new TimeLocatedValue(0, TimeStamp.Zero());
        }

        public override string ToString() => Name;

        public double Value
        {
            get { return _value?.Value ?? double.NaN; }
            set { TimeValue = new TimeLocatedValue(value, this.Parent?.GetCurrentClockTime() ?? TimeStamp.Zero()); }
        }

        [Browsable(false)]
        public TimeLocatedValue TimeValue {
            get { return _value; }
            set {
                _value = value;
                ((ValueOutputPort) OutputPorts[0]).SendData(value);
            }
        }

        protected override void ProcessingStarted() {
            ((ValueOutputPort) OutputPorts[0]).SendData(new TimeLocatedValue(Value, TimeStamp.Zero()));
        }

        protected override void Serializing(XmlWriter writer) {
            writer.WriteAttributeString("value", Value.ToString(CultureInfo.InvariantCulture));
        }
        
    }
}