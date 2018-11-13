using NodeSystemLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace SimpleADC.Metrics {

    [Metric("Logic Not", "Logic")]
    class MetricLogicNot : Node {

        public MetricLogicNot(XmlNode node, Graph graph) : this(graph) { }

        public MetricLogicNot(Graph graph) : base("Logical Not", graph) {
            InputPort.Create<ValueInputPort>("in", this);
            OutputPort.Create<ValueOutputPort>("out", this);
        }

        protected override void ValueAvailable(ValueInputPort port) {
            TimeLocatedValue val;
            while (port.Values.TryDequeue(out val)) {
                double result = val.Value < 0.5 ? 1 : 0;
                ((ValueOutputPort)OutputPorts[0]).SendData(new TimeLocatedValue(result, val.Stamp));
            }
        }

        public override string ToString() => Name;
    }
}
