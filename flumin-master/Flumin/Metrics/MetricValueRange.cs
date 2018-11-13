using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NodeSystemLib;
using NodeGraphControl;
using System.Drawing;
using System.Xml;
using System.ComponentModel;
using System.Globalization;

namespace SimpleADC.Metrics {

    [Metric("Range", "Values")]
    class MetricValueRange : Node, INodeUi {

        private NodeGraphControl.Controls.NodeControlSlider _slider;
        private ValueOutputPort _portOut;
        private TimeLocatedValue _value;
        private double _max;
        private double _min;
        private int _steps;

        public MetricValueRange(XmlNode node, Graph graph) : this(graph) {
            Max   = double.Parse(node.Attributes?.GetNamedItem("max")?.Value   ?? "10");
            Min   = double.Parse(node.Attributes?.GetNamedItem("min")?.Value   ?? "0");
            Steps = int.Parse(node.Attributes?.GetNamedItem("steps")?.Value    ?? "100");
            Value = double.Parse(node.Attributes?.GetNamedItem("value")?.Value ?? "0");
        }

        public MetricValueRange(Graph graph) : base("Range", graph, InputPort.CreateMany(), OutputPort.CreateMany(OutputPort.Create("Out", PortDataType.Value))) {
            _portOut = (ValueOutputPort)OutputPorts[0];
        }

        public override string ToString() => Name;

        public int Steps
        {
            get
            {
                return _steps;
            }
            set
            {
                _steps = value;
                if (_slider != null) _slider.Steps = value;
            }
        }

        public double Max {
            get {
                return _max;
            }
            set
            {
                _max = value;
                if (_slider != null) _slider.Max = value;
            }
        }

        public double Min {
            get {
                return _min;
            }
            set {
                _min = value;
                if (_slider != null) _slider.Min = value;
            }
        }

        public double Value
        {
            get {
                return _value?.Value ?? double.NaN;
            }
            set {
                TimeValue = new TimeLocatedValue(value, Parent?.GetCurrentClockTime() ?? new TimeStamp(0));
            }
        }

        [Browsable(false)]
        public TimeLocatedValue TimeValue
        {
            get { return _value; }
            set
            {
                _value = value;
                if (_slider != null) _slider.Value = _value.Value;
                ((ValueOutputPort)OutputPorts[0]).SendData(_value);
            }
        }

        protected override void ProcessingStarted() {
            ((ValueOutputPort)OutputPorts[0]).SendData(_value);
        }

        protected override void Serializing(XmlWriter writer) {
            writer.WriteAttributeString("value", Value.ToString(CultureInfo.InvariantCulture));
            writer.WriteAttributeString("max",   Max.ToString(CultureInfo.InvariantCulture));
            writer.WriteAttributeString("min",   Min.ToString(CultureInfo.InvariantCulture));
            writer.WriteAttributeString("steps", Steps.ToString());
        }

        public void OnDoubleClick() {
            //
        }

        public void OnDraw(Rectangle node, Graphics e) {
            //
        }

        public void OnLoad(NodeGraphNode node) {
            _slider               = new NodeGraphControl.Controls.NodeControlSlider(node);
            _slider.Position      = new Point(30, 10);
            _slider.Size          = new Size(70, 20);
            _slider.ValueChanged += Slider_ValueChanged;
            _slider.Max           = Max;
            _slider.Min           = Min;
            _slider.Value         = Value;
            _slider.Steps         = Steps;

            node.Controls.Add(_slider);
        }

        private void Slider_ValueChanged(object sender, EventArgs e) {
            Value = _slider.Value;
        }
    }

}
