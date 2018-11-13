using NodeSystemLib2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Drawing;
using EC = NodeEditorLib.EditorControl;

namespace Flumin.Metrics {

    //[Metric("Unknown node", "Unknown")]
    class MetricUnknownNode : Node, NodeSystemLib2.Generic.INodeUi {

        private readonly XmlNode data;

        public override bool CanProcess => false;
        public override bool CanTransfer => false;

        public MetricUnknownNode(XmlNode node, Graph g) : this(g) {
            data = node.CloneNode(deep: true);

            var inputs = data.SelectSingleNode("InputPorts");
            var outputs = data.SelectSingleNode("OutputPorts");

            int inputCounter = 0;
            int outputCounter = 0;

            foreach (var input in inputs.ChildNodes.OfType<XmlNode>()) {
                var name = input.TryGetAttribute("name", "Inp" + (inputCounter));
                var type = (PortDataType)Enum.Parse(typeof(PortDataType), input.TryGetAttribute("type", ""));
                //var port = InputPort.Create(name, type);
                //AddInput(port);
                ++inputCounter;
            }

            foreach (var output in outputs.ChildNodes.OfType<XmlNode>()) {
                var name = output.TryGetAttribute("name", "Out" + (outputCounter));
                var type = (PortDataType)Enum.Parse(typeof(PortDataType), output.TryGetAttribute("type", ""));
                //var port = OutputPort.Create(name, type);
                //AddOutput(port);
                ++outputCounter;
            }
        }

        public MetricUnknownNode(Graph g) : base(g) {
            Name = "Unknown Node";
        }

        protected override void Serializing(XmlWriter writer) {
            foreach (var attr in data.Attributes.OfType<XmlAttribute>()) {
                writer.WriteAttributeString(attr.Name, attr.Value);
            }

            foreach (var child in data.ChildNodes.OfType<XmlNode>()) {
                child.WriteTo(writer);
            }
        }

        public void OnLoad(EC.Node node) {
            //
        }

        public void OnDoubleClick() {
            //
        }

        public void OnDraw(Rectangle node, Graphics e) {
            e.DrawRectangle(Pens.Red, node);
        }

        public override void PrepareProcessing() {
            throw new InvalidOperationException($"Metric not found: {Name}");
        }

        public override void StartProcessing() {
            throw new NotImplementedException();
        }

        public override void StopProcessing() {
            throw new NotImplementedException();
        }

        public override void SuspendProcessing() {
            throw new NotImplementedException();
        }

        public override void Process() {
            throw new NotImplementedException();
        }

        public override void Transfer() {
            throw new NotImplementedException();
        }
    }

}
