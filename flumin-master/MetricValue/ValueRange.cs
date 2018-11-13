using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NodeSystemLib2;
using NodeSystemLib2.Generic;
using NodeSystemLib2.Generic.NodeAttributes;
using NodeSystemLib2.FormatValue;
using NodeGraphControl;
using System.Drawing;
using System.Xml;
using System.ComponentModel;
using System.Globalization;

namespace MetricValue {


    [Metric("Range", "Values")]
    public class MetricValueRange : StateNode<MetricValueRange>, INodeUi {

        private NodeGraphControl.Controls.NodeControlSlider _slider;
        private readonly OutputPortValueDouble _portOut;
        private readonly AttributeValueDouble _attrValue;
        private readonly AttributeValueDouble _attrMax;
        private readonly AttributeValueDouble _attrMin;
        private readonly AttributeValueInt _attrSteps;

        public MetricValueRange(XmlNode node, Graph graph) : this(graph) {
            Deserializing(node);
        }

        public MetricValueRange(Graph graph) : base("Range", graph) {
            _portOut = new OutputPortValueDouble(this, "Out");

            _attrValue = new AttributeValueDouble(this, "Value", IsRunning);
            _attrMax = new AttributeValueDouble(this, "Max", IsRunning);
            _attrMin = new AttributeValueDouble(this, "Min", IsRunning);
            _attrSteps = new AttributeValueInt(this, "Steps", IsRunning);

            _attrValue.Changed += (s, e) => {
                if (State == Graph.State.Running) SendValue(_attrValue.TypedGet());
            };

            _attrMax.Changed += (s, e) => {
                if (_slider != null) _slider.Max = _attrMax.TypedGet();
            };

            _attrMin.Changed += (s, e) => {
                if (_slider != null) _slider.Min = _attrMin.TypedGet();
            };

            _attrSteps.Changed += (s, e) => {
                if (_slider != null) _slider.Steps = _attrSteps.TypedGet();
            };
        }

        public override string ToString() => Name;

        public override bool CanProcess => false;
        public override bool CanTransfer => _portOut.BufferedValueCount > 0;

        public override void StartProcessing() {
            SendValue(_attrValue.TypedGet());
        }

        private void SendValue(double value) {
            ((OutputPortValueDouble)OutputPorts[0]).BufferForTransfer(new TimeLocatedValue<double>(value, Parent?.GetCurrentClockTime() ?? new TimeStamp(0)));
        }

        public void OnDoubleClick() {
            //
        }

        public void OnDraw(Rectangle node, Graphics e) {
            //
        }

        public void OnLoad(NodeGraphNode node) {
            _slider = new NodeGraphControl.Controls.NodeControlSlider(node);
            _slider.Position = new Point(30, 10);
            _slider.Size = new Size(70, 20);
            _slider.ValueChanged += Slider_ValueChanged;
            _slider.Max = _attrMax.TypedGet();
            _slider.Min = _attrMin.TypedGet();
            _slider.Value = _attrValue.TypedGet();
            _slider.Steps = _attrSteps.TypedGet();

            node.Controls.Add(_slider);
        }

        private void Slider_ValueChanged(object sender, EventArgs e) {
            _attrValue.Set(_slider.Value);
        }

        public override void PrepareProcessing() {
            _portOut.PrepareProcessing();
        }

        public override void StopProcessing() {}
        public override void SuspendProcessing() {}
        public override void Process() {}

        public override void Transfer() {
            _portOut.TransferBuffer();
        }
    }


}
