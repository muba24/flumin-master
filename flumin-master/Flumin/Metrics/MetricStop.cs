using System.Xml;
using NodeSystemLib;

namespace SimpleADC.Metrics {

    [Metric("Graph Stop", "Other")]
    class MetricStop : Node {

        public MetricStop(XmlNode node, Graph graph) : this(graph) {
        }

        public MetricStop(Graph graph) : base("Stop", graph) {
            var port = InputPort.Create<EventInputPort>("inp", this);
            port.EventRaised += Port_EventRaised;
        }

        private void Port_EventRaised(object sender, EventInputPort.EventEventArgs e) {
            System.Diagnostics.Debug.WriteLine("Stop request for graph time " + e.Stamp);
            System.Diagnostics.Debug.WriteLine("Current time: " + Parent.GetCurrentClockTime());
            GlobalSettings.Instance.StopProcessing();
        }

    }

}
