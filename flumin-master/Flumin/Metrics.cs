using System;
using System.ComponentModel;
using System.Linq;
using DeviceLibrary;
using SimpleADC.NodeSystem;

namespace SimpleADC {

    public class DeviceNode : Node {

        public IDevicePort Port { get; }

        public override string ToString() => Port.Name;

        public DeviceNode(IDevicePort port)
            : base("Dev Port",
                InputPort.CreateMany(),
                OutputPort.CreateMany(OutputPort.Create("out", PortDataType.Array)))
        {
            Port = port;
            Port.OnBufferReady += Port_OnBufferReady;
            Port.SamplerateChanged += Port_SamplerateChanged;

            OutputPorts[0].Samplerate = Port.Samplerate;
        }

        protected override void ProcessingStarted() {
            Port.Status = DevicePortStatus.Active;
        }

        protected override void ProcessingStopped() {
            Port.Status = DevicePortStatus.Idle;
        }

        private void Port_SamplerateChanged(IDevicePort sender, int newRate) {
            OutputPorts[0].Samplerate = newRate;
        }

        private void Port_OnBufferReady(IDevicePort sender, double[] buffer) {
            ((DataOutputPort)OutputPorts[0]).SendData(buffer);
        }

    }

    class MetricRms : Node {

        public MetricRms() : base("Number Sink",
                                  InputPort.CreateMany(InputPort.Create("In", PortDataType.Array),
                                                       InputPort.Create("Sqr", PortDataType.Value)),
                                  OutputPort.CreateMany()) {
        }

        private double[] _buffer;

        private bool _sqrt;

        protected override void InputSamplerateChanged(InputPort e) {
            _buffer = new double[e.Samplerate / 10];
        }

        protected override void ValueAvailable(ValueInputPort port) {
            _sqrt = port.Value > 0.5;
        }

        protected override void DataAvailable() {
            while (((DataInputPort)InputPorts[0]).Buffer.Length > _buffer.Length) {
                Process();
            }
        }

        private void Process() {
            ((DataInputPort)InputPorts[0]).Buffer.Dequeue(_buffer, 0, _buffer.Length);
            var rms = _buffer.Sum(t => t * t) / _buffer.Length;
            Console.WriteLine(@"Recieved buffer RMS: {0}", _sqrt ? Math.Sqrt(rms) : rms);
        }

    }

    class MetricFilter : Node {

        private readonly Biquad _bpf = new Biquad(Biquad.BiquadType.Bandpass, 1, 1, 0);
        private double _fc;

        public MetricFilter()
            : base("Filter",
                   InputPort.CreateMany(InputPort.Create("in", PortDataType.Array)),
                   OutputPort.CreateMany(OutputPort.Create("out", PortDataType.Array))) {

            Fc = 100000;
        }

        public Biquad.BiquadType FilterType {
            get { return _bpf.Type; }
            set { _bpf.Type = value; }
        }

        [Description("Eck- bzw. Bandmittenfrequenz")]
        public double Fc {
            get { return _fc; }
            set {
                _fc = value;
                if (OutputPorts[0].Samplerate > 0) {
                    _bpf.Fc = _fc/OutputPorts[0].Samplerate;
                }
            }
        }

        [Description("Bandbreite")]
        public double Q {
            get { return _bpf.Q; }
            set { _bpf.Q = value; }
        }

        [Description("Peak/Shelf Gain")]
        public double Gain {
            get { return _bpf.PeakGainDb; }
            set { _bpf.PeakGainDb = value; }
        }

        private double[] _inputBuffer;
        private double[] _outputBuffer;
        protected override void InputSamplerateChanged(InputPort e) {
            _inputBuffer = new double[e.Samplerate/10];
            _outputBuffer = new double[_inputBuffer.Length];

            OutputPorts[0].Samplerate = e.Samplerate;
            _bpf.Fc = Fc/e.Samplerate;
        }

        protected override void DataAvailable() {
            while (((DataInputPort) InputPorts[0]).Buffer.Length > _inputBuffer.Length) {
                Process();
            }
        }

        public void Process() {
            ((DataInputPort) InputPorts[0]).Buffer.Dequeue(_inputBuffer, 0, _inputBuffer.Length);

            for (var i = 0; i < _inputBuffer.Length; i++) {
                _outputBuffer[i] = _bpf.Process(_inputBuffer[i]);
            }

            ((DataOutputPort) OutputPorts[0]).SendData(_outputBuffer);
        }

        public override string ToString() {
            return Name;
        }
    }

    class MetricPower : Node {

        private double[] _inputBuffer;
        private double[] _outputBuffer;
        private int _sumSize = 50;

        public int SumSize {
            get { return _sumSize; }
            set { _sumSize = value; CreateOutputBuffer(); }
        }

        public MetricPower()
            : base("Power",
                InputPort.CreateMany(InputPort.Create("in", PortDataType.Array)),
                OutputPort.CreateMany(OutputPort.Create("out", PortDataType.Array)))
        {
            
        }

        private void CreateOutputBuffer() {
            _outputBuffer = new double[_inputBuffer.Length / SumSize];
        }

        protected override void InputSamplerateChanged(InputPort e) {
            _inputBuffer = new double[e.Samplerate/10];
            CreateOutputBuffer();
            OutputPorts[0].Samplerate = InputPorts[0].Samplerate;
        }

        protected override void DataAvailable() {
            while (((DataInputPort)InputPorts[0]).Buffer.Length > _inputBuffer.Length) {
                Process();
            }
        }

        public void Process() {
            ((DataInputPort)InputPorts[0]).Buffer.Dequeue(_inputBuffer, 0, _inputBuffer.Length);

            for (var i = 0; i < _inputBuffer.Length; i += _sumSize) {
                var sum = 0.0;
                for (var j = i; j < i + _sumSize; j++) {
                    sum += _inputBuffer[j]*_inputBuffer[j];
                }
                _outputBuffer[i / _sumSize] = Math.Sqrt(sum);
            }

            ((DataOutputPort)OutputPorts[0]).SendData(_outputBuffer);
        }

        public override string ToString() {
            return Name;
        }

    }


    class MetricSlowProcess : Node {

        private readonly Random _rnd = new Random();

        private double[] _buffer;

        public MetricSlowProcess()
        : base("Slow Process",
               InputPort.CreateMany(InputPort.Create("in", PortDataType.Array)),
               OutputPort.CreateMany(OutputPort.Create("out", PortDataType.Array))) {

        }

        protected override void InputSamplerateChanged(InputPort e) {
            OutputPorts[0].Samplerate = InputPorts[0].Samplerate;
            _buffer = new double[e.Samplerate/10];
        }

        public override string ToString() {
            return Name;
        }

        protected override void DataAvailable() {
            while (((DataInputPort)InputPorts[0]).Buffer.Length > _buffer.Length) {
                Process();
            }
        }

        public void Process() {
            ((DataInputPort) InputPorts[0]).Buffer.Dequeue(_buffer, 0, _buffer.Length);

            if (_rnd.NextDouble() > 0.9) {
                System.Threading.Thread.Sleep(500);
            }

            ((DataOutputPort) OutputPorts[0]).SendData(_buffer);
        }

    }

    class MetricHalfrate : Node {

        private double[] _inputBuffer;
        private double[] _outputBuffer;

        public MetricHalfrate()
        : base("Half Rate",
               InputPort.CreateMany(InputPort.Create("in", PortDataType.Array)),
               OutputPort.CreateMany(OutputPort.Create("out", PortDataType.Array))) {

        }

        protected override void InputSamplerateChanged(InputPort e) {
            OutputPorts[0].Samplerate = InputPorts[0].Samplerate / 2;
            _inputBuffer = new double[InputPorts[0].Samplerate/10];
            _outputBuffer = new double[_inputBuffer.Length / 2];
        }

        public override string ToString() {
            return Name;
        }

        protected override void DataAvailable() {
            while (((DataInputPort)InputPorts[0]).Buffer.Length > _inputBuffer.Length) {
                Process();
            }
        }

        public void Process() {
            ((DataInputPort)InputPorts[0]).Buffer.Dequeue(_inputBuffer, 0, _inputBuffer.Length);

            for (var i = 0; i < _outputBuffer.Length; i++) {
                _outputBuffer[i] = _inputBuffer[i * 2];
            }

            ((DataOutputPort)OutputPorts[0]).SendData(_outputBuffer);
        }

    }

    class MetricAdder : Node {

        private double[] _inputBufferA;
        private double[] _inputBufferB;
        private double[] _outputBuffer;

        public MetricAdder()
            : base("Adder",
               InputPort.CreateMany(InputPort.Create("inA", PortDataType.Array),
                                    InputPort.Create("inB", PortDataType.Array)),
               OutputPort.CreateMany(OutputPort.Create("out", PortDataType.Array))) {

        }

        protected override void InputSamplerateChanged(InputPort e) {
            if (InputPorts[0].Samplerate == InputPorts[1].Samplerate) {
                OutputPorts[0].Samplerate = e.Samplerate;
                _inputBufferA = new double[e.Samplerate / 10];
                _inputBufferB = new double[e.Samplerate / 10];
                _outputBuffer = new double[e.Samplerate / 10];
            } else {
                OutputPorts[0].Samplerate = 0;
            }
        }

        public override string ToString() {
            return Name;
        }

        [Description("Zweiten Input von erstem subtrahieren")]
        public bool Subtract { get; set; }

        protected override void DataAvailable() {
            if (_inputBufferA == null || _inputBufferB == null) return;

            while (((DataInputPort)InputPorts[0]).Buffer.Length > _inputBufferA.Length &&
                   ((DataInputPort)InputPorts[1]).Buffer.Length > _inputBufferB.Length) {

                Process();
            }
        }

        public void Process() {
            ((DataInputPort)InputPorts[0]).Buffer.Dequeue(_inputBufferA, 0, _inputBufferA.Length);
            ((DataInputPort)InputPorts[1]).Buffer.Dequeue(_inputBufferB, 0, _inputBufferB.Length);

            if (Subtract) {
                for (var i = 0; i < _inputBufferA.Length; i++) {
                    _outputBuffer[i] = _inputBufferA[i] - _inputBufferB[i];
                }
            } else {
                for (var i = 0; i < _inputBufferA.Length; i++) {
                    _outputBuffer[i] = _inputBufferA[i] + _inputBufferB[i];
                }
            }

            ((DataOutputPort)OutputPorts[0]).SendData(_outputBuffer);
        }

    }

}
