using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using NodeSystemLib2;
using NodeSystemLib2.FormatData1D;

namespace DeviceLibrary {

    public class DeviceSimulator : IMetricFactory {

        const int NumberOfPorts = 3;

        public int Count => NumberOfPorts;

        public Guid Id => new Guid("5efcf832-2138-4d97-bd04-b38972fdf493");

        public Node CreateInstance(string uniqueId, Graph g) {
            int index = 0;
            if (int.TryParse(uniqueId, out index)) {
                return CreateInstance(index, g);
            }
            throw new KeyNotFoundException();
        }

        public Node CreateInstance(int index, Graph g) {
            if (!Enumerable.Range(0, NumberOfPorts).Contains(index)) {
                throw new IndexOutOfRangeException();
            }
            return new DeviceSimulatorPort(g, index);
        }

        public Node CreateInstance(string uniqueId, Graph g, XmlNode node) {
            int index = 0;
            if (int.TryParse(uniqueId, out index)) {
                return CreateInstance(index, g, node);
            }
            throw new KeyNotFoundException();
        }

        public Node CreateInstance(int index, Graph g, XmlNode node) {
            if (!Enumerable.Range(0, NumberOfPorts).Contains(index)) {
                throw new IndexOutOfRangeException();
            }
            return new DeviceSimulatorPort(g, index, node);
        }

        public MetricMetaData GetMetricInfo(int index) {
            return new MetricMetaData($"Simulator Port #{index}", "Simulator", index.ToString());
        }

        public void SaveInternalState(Graph g, XmlWriter writer) {
            //
        }

        public void SetFactorySettings(Graph g, XmlNode factorySettings) {
            //
        }
    }

    [Metric("Device Simulator Port", "Simulator", instantiable: false, uniqueInGraph: true)]
    class DeviceSimulatorPort : NodeSystemLib2.Generic.StateNode<DeviceSimulatorPort> {

        readonly OutputPortData1D _portOut;

        public enum SignalType {
            Sine,
            Triangle,
            Rectangle
        }

        private readonly Dictionary<SignalType, Func<double, double>> Generators = new Dictionary<SignalType, Func<double, double>> {
            { SignalType.Sine,      (phase) => Math.Sin(phase) },
            { SignalType.Triangle,  (phase) => ((phase % (2 * Math.PI)) - Math.PI) / Math.PI },
            { SignalType.Rectangle, (phase) => ((phase % (2 * Math.PI))) / Math.PI > 1 ? 1 : 0 }
        };

        private readonly NodeSystemLib2.Generic.NodeAttributes.AttributeValueDouble _attrFrequency;
        private readonly NodeSystemLib2.Generic.NodeAttributes.AttributeValueDouble _attrAmplitude;
        private readonly NodeSystemLib2.Generic.NodeAttributes.AttributeValueDouble _attrPhase;
        private readonly NodeSystemLib2.Generic.NodeAttributes.AttributeValueInt _attrSamplerate;
        private readonly NodeSystemLib2.Generic.NodeAttributes.AttributeValueInt _attrPeriod;
        private readonly NodeSystemLib2.Generic.NodeAttributes.AttributeValueEnum<SignalType> _attrSignalType;

        private PrecisionTimer              _precTimer;
        private double[]                    _buffer;

        private double                      _phaseStep;
        private double                      _phaseAcc;
        
        protected int Id { get; }

        public override bool CanProcess => false;
        public override bool CanTransfer => _portOut.Buffer?.Available > 0;

        public DeviceSimulatorPort(Graph g, int portId, XmlNode node) : this(g, portId) {
            Deserializing(node);
        }

        public DeviceSimulatorPort(Graph g, int portId) : base("Simulator Port", g, UniquenessBy(portId)) {
            Id       = portId;

            _portOut = new OutputPortData1D(this, "Out");

            _attrFrequency = new NodeSystemLib2.Generic.NodeAttributes.AttributeValueDouble(this, "Frequency", "Hz");
            _attrAmplitude = new NodeSystemLib2.Generic.NodeAttributes.AttributeValueDouble(this, "Amplitude");
            _attrSamplerate = new NodeSystemLib2.Generic.NodeAttributes.AttributeValueInt(this, "Samplerate", "Hz");
            _attrSignalType = new NodeSystemLib2.Generic.NodeAttributes.AttributeValueEnum<SignalType>(this, "Type");
            _attrPeriod = new NodeSystemLib2.Generic.NodeAttributes.AttributeValueInt(this, "Period", "ms");
            _attrPhase = new NodeSystemLib2.Generic.NodeAttributes.AttributeValueDouble(this, "Phase", "°");

            _attrFrequency.Changed += (s, e) => CalculatePhaseStep();
            _attrSamplerate.Changed += (s, e) => _portOut.Samplerate = _attrSamplerate.TypedGet();

            _attrSamplerate.SetRuntimeReadonly();
            _attrPeriod.SetRuntimeReadonly();

            _attrFrequency.Set(100000);
            _attrAmplitude.Set(1);
            _attrPeriod.Set(100);
            _attrSamplerate.Set(1000000);
        }

        public override void PrepareProcessing() {
            if (_precTimer == null) {
                _precTimer = new PrecisionTimer();
                _precTimer.Elapsed += precTimer_Elapsed;
                _precTimer.Milliseconds = _attrPeriod.TypedGet();
                _precTimer.ToleranceMilliseconds = 10;
            }

            var sampleCount = _attrSamplerate.TypedGet() * _attrPeriod.TypedGet() / 1000;
            if (_buffer == null || _buffer.Length != sampleCount) {
                _buffer = new double[sampleCount];
            }

            _portOut.PrepareProcessing(
                DefaultParameters.DefaultQueueMilliseconds.ToSamples(_portOut.Samplerate),
                DefaultParameters.DefaultBufferMilliseconds.ToSamples(_portOut.Samplerate)
            );

            CalculatePhaseStep();
            _phaseAcc = 0;
        }

        public override void SuspendProcessing() {
            _precTimer.Enabled = false;
        }

        public override void StopProcessing() {
            _precTimer.Enabled = false;
        }

        public override void StartProcessing() {
            _precTimer.Enabled = true;
        }

        private void precTimer_Elapsed(object sender, int elapsed) {
            var gen = Generators[_attrSignalType.TypedGet()];
            var ampl = _attrAmplitude.TypedGet();
            var phase = _attrPhase.TypedGet() * Math.PI / 180;

            for (int i = 0; i < _buffer.Length; i++) {
                _buffer[i] = ampl * gen(_phaseAcc + phase);
                _phaseAcc += _phaseStep;
            }

            var written = _portOut.Buffer.Write(_buffer, 0, _buffer.Length);
            if (written != _buffer.Length) {
                StopProcessing();
                Parent.AsyncEmergencyStop(this);
                System.Diagnostics.Debug.WriteLine("DeviceSim: Not enough space in output buffer. Aborting");
            }
        }

        private void CalculatePhaseStep() {
            if (_attrSamplerate.TypedGet() > 0) {
                _phaseStep = 2.0 * Math.PI * _attrFrequency.TypedGet() / _attrSamplerate.TypedGet();
            }
        }

        private static Func<DeviceSimulatorPort, bool> UniquenessBy(int id) => 
            (p) => p.Id == id;

        public override void Process() {}

        public override void Transfer() {
            _portOut.Transfer();
        }
    }

}
