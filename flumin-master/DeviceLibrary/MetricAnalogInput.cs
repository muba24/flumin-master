using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NodeSystemLib2;
using NodeSystemLib2.Generic;
using NodeSystemLib2.Generic.NodeAttributes;
using System.Runtime.InteropServices;
using System.Xml;
using System.ComponentModel;
using System.Drawing;

namespace DeviceLibrary {

    [Metric("AnalogIn", "Nidaq", instantiable: false, uniqueInGraph: true)]
    class MetricAnalogInput : StateNode<MetricAnalogInput>, INidaqMetric, IMetricInput, INodeUi {

        readonly NodeSystemLib2.FormatData1D.OutputPortData1D _portOut;

        public NidaqSingleton.Device Device { get; }
        public NidaqSingleton.Channel Channel { get; }
        public NidaqSession Session { get; }

        public double VMax => _attrVMax.TypedGet();
        public double VMin => _attrVMin.TypedGet();
        public NidaQmxHelper.TerminalCfg TerminalConfig => _attrTerminalCfg.TypedGet();

        private AttributeValueDouble _attrVMax;
        private AttributeValueDouble _attrVMin;
        private AttributeValueEnum<NidaQmxHelper.TerminalCfg> _attrTerminalCfg;

        public NidaqSingleton.Channel.ChannelType Type => NidaqSingleton.Channel.ChannelType.AnalogIn;

        public int Samplerate {
            get { return _portOut.Samplerate; }
            set { _portOut.Samplerate = value; }
        }

        public override bool CanProcess => false;
        public override bool CanTransfer => _portOut.Buffer.Available > 0;

        public MetricAnalogInput(NidaqSingleton.Device device, NidaqSingleton.Channel channel, Graph g, XmlNode node) : this(device, channel, g) {
            _attrVMax.Deserialize(node.Attributes.GetNamedItem(_attrVMax.Name)?.Value ?? "10");
            _attrVMin.Deserialize(node.Attributes.GetNamedItem(_attrVMin.Name)?.Value ?? "-10");
            _attrTerminalCfg.Deserialize(node.Attributes.GetNamedItem(_attrTerminalCfg.Name)?.Value ?? NidaQmxHelper.TerminalCfg.RSE.ToString());
        }

        public MetricAnalogInput(NidaqSingleton.Device device, NidaqSingleton.Channel channel, Graph g) 
            : base("AI " + channel.Path, g, UniquenessBy(channel)) {

            _portOut    = new NodeSystemLib2.FormatData1D.OutputPortData1D(this, "Out");
            Channel     = channel;
            Device      = device;

            // only add to session after the initialization is done
            Session     = NidaqSingleton.Instance.AddToSession(this);

            _attrVMax = new AttributeValueDouble(this, "Vmax");
            _attrVMin = new AttributeValueDouble(this, "Vmin");
            _attrTerminalCfg = new AttributeValueEnum<NidaQmxHelper.TerminalCfg>(this, "TerminalConfig");

            _attrVMax.SetRuntimeReadonly();
            _attrVMin.SetRuntimeReadonly();
            _attrTerminalCfg.SetRuntimeReadonly();
        }

        private static Func<MetricAnalogInput, bool> UniquenessBy(NidaqSingleton.Channel channel) =>
            p => p.Channel == channel && p.Type == NidaqSingleton.Channel.ChannelType.AnalogIn;

        public void SetBufferSize(int samples) {
            _portOut.PrepareProcessing(
                10 * samples,
                DefaultParameters.DefaultBufferMilliseconds.ToSamples(_portOut.Samplerate)
            );
        }

        public void DistributeData(IntPtr pData, int samples) {
            if (State != Graph.State.Running) return;

            _portOut.Buffer.Write(pData, 0, samples);

            //if (Parent.ClockSynchronizationNode == this) {
            //    Parent.SynchronizeClock(_outputBuffer.CurrentTime);
            //}
        }

        public override void PrepareProcessing() {
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

        protected override void Serializing(XmlWriter writer) {
            writer.WriteAttributeString(_attrVMax.Name, _attrVMax.Serialize());
            writer.WriteAttributeString(_attrVMin.Name, _attrVMin.Serialize());
            writer.WriteAttributeString(_attrTerminalCfg.Name, _attrTerminalCfg.Serialize());
            base.Serializing(writer);
        }

        public override void Dispose() {
            Session.UnregisterNode(this);
            base.Dispose();
        }

        public void OnLoad(NodeEditorLib.EditorControl.Node node) {
        }

        public void OnDoubleClick() {
            if (State == Graph.State.Stopped) {
                var task = Session.GetTask<NidaqSessionAnalogIn>(Channel.Device);
                using (var dlg = new SettingsAnalogIn(task.ClockRate)) {
                    var result = dlg.ShowDialog();
                    if (result == System.Windows.Forms.DialogResult.OK) {
                        task.ClockRate = dlg.ClockRate;
                        Samplerate = task.ClockRate;
                    }
                }
            }
        }

        public void OnDraw(Rectangle node, Graphics e) {
        }

        public override void Process() {}

        public override void Transfer() {
            _portOut.Transfer();
        }
    }
}
