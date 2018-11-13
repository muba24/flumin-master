using NodeSystemLib2;
using NodeSystemLib2.Generic;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Drawing;

namespace DeviceLibrary {

    [Metric("AnalogOut", "Nidaq", instantiable:false, uniqueInGraph:true)]
    class MetricAnalogOutput : StateNode<MetricAnalogOutput>, INidaqMetric, INodeUi {

        readonly NodeSystemLib2.FormatData1D.InputPortData1D _portIn;

        public MetricAnalogOutput(NidaqSingleton.Device device, NidaqSingleton.Channel channel, Graph g, XmlNode node) : this(device, channel, g) {
            VMax = double.Parse(node.Attributes.GetNamedItem("vmax")?.Value ?? "10", System.Globalization.CultureInfo.InvariantCulture);
            VMin = double.Parse(node.Attributes.GetNamedItem("vmin")?.Value ?? "-10", System.Globalization.CultureInfo.InvariantCulture);
        }

        public MetricAnalogOutput(NidaqSingleton.Device device, NidaqSingleton.Channel channel, Graph g)
            :base("AO " + channel.Path, g, UniquenessBy(channel)) {

            _portIn = new NodeSystemLib2.FormatData1D.InputPortData1D(this, "In");
            Channel = channel;
            Device = device;

            Session = NidaqSingleton.Instance.AddToSession(this);
        }

        private static Func<MetricAnalogOutput, bool> UniquenessBy(NidaqSingleton.Channel channel) =>
            p => p.Channel == channel && p.Type == NidaqSingleton.Channel.ChannelType.AnalogOut;

        [Browsable(false)]
        public NidaqSingleton.Channel Channel { get; }

        [Browsable(false)]
        public NidaqSingleton.Device Device { get; }

        [Browsable(false)]
        public NidaqSession Session { get; }

        [Browsable(false)]
        public NidaqSingleton.Channel.ChannelType Type => NidaqSingleton.Channel.ChannelType.AnalogOut;

        [Browsable(false)]
        public int Samplerate {
            get { return _portIn.Samplerate; }
            set { _portIn.Samplerate = value; }
        }

        [Browsable(false)]
        public int ChannelNumber { get; set; }

        public double VMax { get; set; } = 10;
        public double VMin { get; set; } = -10;

        public override bool CanProcess => _portIn.Available > 0;
        public override bool CanTransfer => false;

        public override void PrepareProcessing() {
            _portIn.PrepareProcessing(
                DefaultParameters.DefaultQueueMilliseconds.ToSamples(_portIn.Samplerate),
                DefaultParameters.DefaultBufferMilliseconds.ToSamples(_portIn.Samplerate)
            );

            Session.SetNodeState(this, NidaqSession.NodeState.Prepared);
        }

        public override void StartProcessing() {
            Session.SetNodeState(this, NidaqSession.NodeState.Running);
        }

        public override void SuspendProcessing() {
            Session.SetNodeState(this, NidaqSession.NodeState.Suspended);
        }

        public override void StopProcessing() {
            Session.SetNodeState(this, NidaqSession.NodeState.Stopped);
        }

        public override void Dispose() {
            Session.UnregisterNode(this);
            base.Dispose();
        }

        protected override void Serializing(XmlWriter writer) {
            writer.WriteAttributeString("vmax", VMax.ToString(System.Globalization.CultureInfo.InvariantCulture));
            writer.WriteAttributeString("vmin", VMin.ToString(System.Globalization.CultureInfo.InvariantCulture));
            base.Serializing(writer);
        }

        public override void Process() {
            var buffer = _portIn.Read();
            Session.AnalogWrite(Channel.Device, ChannelNumber, buffer.Data, 0, buffer.Available);
        }

        public void OnLoad(NodeEditorLib.EditorControl.Node node) { }
        public void OnDraw(Rectangle node, Graphics e) { }
        public override void Transfer() { }

        public void OnDoubleClick() {
            if (State == Graph.State.Stopped) {
                var task = Session.GetTask<NidaqSessionAnalogOut>(Channel.Device);
                using (var dlg = new SettingsAnalogOut(task.BufferLengthMs, task.PrebufferLengthMs)) {
                    var result = dlg.ShowDialog();
                    if (result == System.Windows.Forms.DialogResult.OK) {
                        task.BufferLengthMs = dlg.BufferLength;
                        task.PrebufferLengthMs = dlg.PreBufferLength;
                    }
                }
            }
        }

    }

}
