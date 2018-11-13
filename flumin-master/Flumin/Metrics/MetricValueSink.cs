using System;
using System.Drawing;
using System.Xml;
using NodeGraphControl;
using NodeSystemLib;

namespace SimpleADC.Metrics {

    [Metric("Value Text Display", "Display")]
    public class MetricValueSink : Node, INodeUi {

        private double LastValue;

        public MetricValueSink(XmlNode node, Graph graph) : this(graph) { }

        public MetricValueSink(Graph graph) : base("Value Sink", graph) {
            InputPort.Create<ValueInputPort>("In", this);
        }

        protected override void ValueAvailable(ValueInputPort port) {
            while (port.Values.Count > 0) {
                TimeLocatedValue value = null;
                if (!port.Values.TryDequeue(out value)) return;
                LastValue = value.Value;
                Console.WriteLine($"Sink got value: {value.Value}");
            }
            UpdateUi();
        }

        public override string ToString() => Name;

        public void OnLoad(NodeGraphNode node) {
        }

        public void OnDoubleClick() {
        }

        public void OnDraw(Rectangle node, Graphics e) {
            PointF loc = new PointF(node.Location.X + node.Width / 2, node.Location.Y + node.Height / 2);
            e.DrawString(Math.Round(LastValue, 5).ToString(), SystemFonts.CaptionFont, Brushes.White, loc);
        }

        private static void UpdateUi() {
            GlobalSettings.Instance.ActiveEditor.BeginInvoke(new Action(GlobalSettings.Instance.ActiveEditor.Refresh));
        }

    }
}