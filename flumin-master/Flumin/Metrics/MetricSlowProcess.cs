using System;
using System.Threading;
using System.Xml;
using NodeSystemLib;

namespace SimpleADC.Metrics {
    class MetricSlowProcess : Node {

        private readonly Random _rnd = new Random();

        private double[] _buffer;

        public MetricSlowProcess(XmlNode node) : this() { }

        public MetricSlowProcess()
            : base("Slow Process",
                InputPort.CreateMany(InputPort.Create("in", PortDataType.Array)),
                OutputPort.CreateMany(OutputPort.Create("out", PortDataType.Array))) {

        }

        protected override void InputSamplerateChanged(InputPort e) {
            OutputPorts[0].Samplerate = InputPorts[0].Samplerate;
            _buffer = new double[e.Samplerate/10];
        }

        public override string ToString() => Name;

        protected override void DataAvailable() {
            while (((DataInputPort)InputPorts[0]).Buffer.Length > _buffer.Length) {
                Process();
            }
        }

        public void Process() {
            ((DataInputPort) InputPorts[0]).Buffer.Dequeue(_buffer, 0, _buffer.Length);

            if (_rnd.NextDouble() > 0.9) {
                Thread.Sleep(500);
            }

            ((DataOutputPort) OutputPorts[0]).SendData(_buffer);
        }

    }
}