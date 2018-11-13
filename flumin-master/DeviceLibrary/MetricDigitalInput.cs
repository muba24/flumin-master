using NodeSystemLib2;
using NodeSystemLib2.Generic;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Drawing;

namespace DeviceLibrary {

    [Metric("DigitalIn", "Nidaq", instantiable: false, uniqueInGraph: true)]
    class MetricDigitalInput : StateNode<MetricAnalogInput>, INidaqMetric, IMetricInput, INodeUi {

        private double[] _scalingBuffer;

        readonly NodeSystemLib2.FormatData1D.OutputPortData1D _portOut;
        readonly int lineNum;

        [Browsable(false)]
        public NidaqSingleton.Device Device { get; }

        [Browsable(false)]
        public NidaqSingleton.Channel Channel { get; }

        [Browsable(false)]
        public NidaqSession Session { get; }

        [Browsable(false)]
        public int Samplerate {
            get { return _portOut.Samplerate; }
            set { _portOut.Samplerate = value; }
        }

        public MetricDigitalInput(NidaqSingleton.Device device, NidaqSingleton.Channel channel, Graph g, XmlNode node) : this(device, channel, g) {
        }

        public MetricDigitalInput(NidaqSingleton.Device device, NidaqSingleton.Channel channel, Graph g) 
            : base("DI " + channel.Path, g, UniquenessBy(channel)) {

            _portOut = new NodeSystemLib2.FormatData1D.OutputPortData1D(this, "Out");
            Channel = channel;
            Device = device;

            lineNum = int.Parse(Channel.Path.Split('/').Last().Last().ToString());

            // only add to session after the initialization is done
            Session = NidaqSingleton.Instance.AddToSession(this);

            _portOut.Samplerate = Session.GetTask<NidaqSessionDigitalIn>(Device).ClockRate;
        }

        private static Func<MetricAnalogInput, bool> UniquenessBy(NidaqSingleton.Channel channel) =>
            p => p.Channel == channel && p.Type == NidaqSingleton.Channel.ChannelType.DigitalIn;

        public NidaqSingleton.Channel.ChannelType Type => NidaqSingleton.Channel.ChannelType.DigitalIn;

        public override bool CanProcess => false;
        public override bool CanTransfer => _portOut.Buffer.Available > 0;

        public void SetBufferSize(int samples) {
            _portOut.PrepareProcessing(
                10 * samples,
                DefaultParameters.DefaultBufferMilliseconds.ToSamples(_portOut.Samplerate)
            );

            _scalingBuffer = new double[samples];
        }

        public void DistributeData(IntPtr pData, int samples) {
            if (State != Graph.State.Running) return;

            unsafe {
                // convert uint32 samples to double
                var p = (uint*)pData.ToPointer();
                for (int i = 0; i < samples; i++) {
                    _scalingBuffer[i] = ((*p++) >> lineNum) & 1;
                }
            }

            _portOut.Buffer.Write(_scalingBuffer, 0, samples);

            //if (Parent.ClockSynchronizationNode == this) {
            //    Parent.SynchronizeClock(_outputBuffer.CurrentTime);
            //}
        }

        public override void PrepareProcessing() {
            Session.SetNodeState(this, NidaqSession.NodeState.Prepared);
        }

        protected override void Serializing(XmlWriter writer) {
            //writer.WriteAttributeString("samplerate", Samplerate.ToString());
            base.Serializing(writer);
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

        public void OnLoad(NodeEditorLib.EditorControl.Node node) {
            //
        }

        public void OnDoubleClick() {
            if (State == Graph.State.Stopped) {
                var task = Session.GetTask<NidaqSessionDigitalIn>(Channel.Device);
                using (var dlg = new SettingsDigitalIn(task.Device, task.Source, task.ClockPath, task.ClockRate)) {
                    var result = dlg.ShowDialog();
                    if (result == System.Windows.Forms.DialogResult.OK) {
                        task.Source = dlg.ClockSource;
                        task.ClockPath = dlg.ClockPath;
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
