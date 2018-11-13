using NodeSystemLib2;
using NodeSystemLib2.Generic;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace DeviceLibrary {

    [Metric("DigitalOut", "Nidaq", instantiable: false, uniqueInGraph: true)]
    class MetricDigitalOutput : StateNode<MetricDigitalOutput>, INidaqMetric, INodeUi {

        readonly NodeSystemLib2.FormatData1D.InputPortData1D _portIn;

        public MetricDigitalOutput(NidaqSingleton.Device device, NidaqSingleton.Channel channel, Graph g, XmlNode node) : this(device, channel, g) {
        }

        public MetricDigitalOutput(NidaqSingleton.Device device, NidaqSingleton.Channel channel, Graph g)
            :base("DO " + channel.Path, g, UniquenessBy(channel)) {

            _portIn = new NodeSystemLib2.FormatData1D.InputPortData1D(this, "Out");
            Channel = channel;
            Device = device;

            Session = NidaqSingleton.Instance.AddToSession(this);
        }

        private static Func<MetricDigitalOutput, bool> UniquenessBy(NidaqSingleton.Channel channel) =>
            p => p.Channel == channel && p.Type == NidaqSingleton.Channel.ChannelType.DigitalOut;

        [Browsable(false)]
        public NidaqSingleton.Channel Channel { get; }

        [Browsable(false)]
        public NidaqSingleton.Device Device { get; }

        [Browsable(false)]
        public NidaqSession Session { get; }

        [Browsable(false)]
        public NidaqSingleton.Channel.ChannelType Type => NidaqSingleton.Channel.ChannelType.DigitalOut;

        [Browsable(false)]
        public int Samplerate {
            get { return _portIn.Samplerate; }
            set { _portIn.Samplerate = value; }
        }

        [Browsable(false)]
        public int ChannelNumber { get; set; }

        public override bool CanProcess => _portIn.Available > 0;
        public override bool CanTransfer => false;

        public override void PrepareProcessing() {
            _portIn.PrepareProcessing(
                DefaultParameters.DefaultQueueMilliseconds.ToSamples(_portIn.Samplerate),
                DefaultParameters.DefaultBufferMilliseconds.ToSamples(_portIn.Samplerate)
            );

            Session.SetNodeState(this, NidaqSession.NodeState.Prepared);
        }

        public  override void StartProcessing() {
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
            base.Serializing(writer);
        }

        public override void Process() {
            var buffer = _portIn.Read();
            Session.DigitalWrite(Channel.Device, ChannelNumber, buffer.Data, 0, buffer.Available);
        }

        public void OnLoad(NodeEditorLib.EditorControl.Node node) {
        }

        public void OnDoubleClick() {
            if (State == Graph.State.Stopped) {
                var task = Session.GetTask<NidaqSessionDigitalOut>(Channel.Device);
                using (var dlg = new SettingsDigitalOut(task.Device, task.ClockPath, task.BufferLengthMs, task.PrebufferLengthMs)) {
                    var result = dlg.ShowDialog();
                    if (result == System.Windows.Forms.DialogResult.OK) {
                        task.ClockPath = dlg.ClockPath;
                        task.BufferLengthMs = dlg.BufferLength;
                        task.PrebufferLengthMs = dlg.PreBufferLength;
                    }
                }
            }
        }

        public void OnDraw(Rectangle node, Graphics e) {
        }

        public override void Transfer() {
            throw new NotImplementedException();
        }
    }
}
