using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NodeGraphControl;
using NodeSystemLib;
using System.Xml;

namespace SimpleADC.Metrics {

    [Metric("TimeStamp Display", "Display")]
    class MetricTimeStampSink : StateNode<MetricTimeStampSink>, INodeUi {

        private TimeStamp LastValue;

        public enum InputType {
            Signal, Value
        }
        InputType type;

        public InputType Type {
            get {
                return type;
            }

            set {
                if (State == ProcessingState.Running) {
                    GlobalSettings.Instance.Errors.Add(new NodeError(this, "Can't change type while running"));
                    return;
                }
                type = value;
                ChangeInputType();
            }
        }

        private void ChangeInputType() {
            if (InputPorts.Count > 0) {
                Parent.Disconnect(InputPorts[0]);
                RemoveInput(InputPorts[0]);
            }

            switch(type) {
                case InputType.Signal:
                    InputPort.Create<DataInputPort>("In", this);
                    break;
                case InputType.Value:
                    InputPort.Create<ValueInputPort>("In", this);
                    break;
                default:
                    GlobalSettings.Instance.Errors.Add(new NodeError(this, "Unknown input type: " + type));
                    break;
            }
        }

        public MetricTimeStampSink(XmlNode node, Graph g) : this(g) {
            Type = (InputType)Enum.Parse(typeof(InputType), node.Attributes?.GetNamedItem("inputtype")?.Value ?? InputType.Signal.ToString());
        }

        protected override void Serializing(XmlWriter writer) {
            writer.WriteAttributeString("inputtype", type.ToString());
            base.Serializing(writer);
        }

        public MetricTimeStampSink(Graph g) : base("TimeStamp Display", g) {
            InputPort.Create<DataInputPort>("In", this);
        }

        public override string ToString() => Name;

        public override bool PrepareProcessing() {
            LastValue = TimeStamp.Zero();
            return base.PrepareProcessing();
        }

        protected override void ValueAvailable(ValueInputPort port) {
            TimeLocatedValue val;
            if (port.Values.TryDequeue(out val)) {
                LastValue = val.Stamp;
                UpdateUi();
            }
        }

        protected override void DataAvailable(DataInputPort port) {
            LastValue = port.Read().CurrentTime;
            UpdateUi();
        }

        public void OnLoad(NodeGraphNode node) {
        }

        public void OnDoubleClick() {
        }

        public void OnDraw(Rectangle node, Graphics e) {
            PointF loc = new PointF(node.Location.X + node.Width / 2, node.Location.Y + node.Height / 2);
            e.DrawString(LastValue.ToString(), SystemFonts.CaptionFont, Brushes.White, loc);
        }

        private static void UpdateUi() {
            GlobalSettings.Instance.ActiveEditor.BeginInvoke(new Action(GlobalSettings.Instance.ActiveEditor.Refresh));
        }
    }
}
