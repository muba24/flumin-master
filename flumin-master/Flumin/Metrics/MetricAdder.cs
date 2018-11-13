using System.ComponentModel;
using System.Xml;
using NodeSystemLib;
using System;

namespace SimpleADC.Metrics {

    [Metric("Adder", "Math")]
    public class MetricAdder : Node {

        private TimeLocatedBuffer _outputBuffer;

        private readonly DataInputPort _portInA;
        private readonly DataInputPort _portInB;
        private readonly DataOutputPort _portOut;

        public MetricAdder(Graph graph) : base("Adder", graph) {
            _portInA = InputPort.Create<DataInputPort>("inA", this);
            _portInB = InputPort.Create<DataInputPort>("inB", this);
            _portOut = OutputPort.Create<DataOutputPort>("out", this);
        }

        public override bool PrepareProcessing() {
            if (_portInA.Samplerate != _portInB.Samplerate) {
                GlobalSettings.Instance.Errors.Add(new Error($"Adder: Input Samplerates not the same"));
                return false;
            }

            if (!base.PrepareProcessing()) return false;
            _outputBuffer = TimeLocatedBuffer.Default(_portInA.Samplerate);
            return true;
        }

        protected override void InputSamplerateChanged(InputPort e) {
            _portOut.Samplerate = _portInA.Samplerate;
        }

        public override FlushState FlushData() {
            var smallest = Math.Min(_portInA.Queue.Length, 
                                    _portInB.Queue.Length);

            if (smallest > 0) {
                ProcessBuffers(smallest);
                return FlushState.Some;
            }

            return FlushState.Empty;
        }

        protected override void DataAvailable(DataInputPort port) {
            if (_portInA.Queue.Length > _portInA.Buffer.Length &&
                _portInB.Queue.Length > _portInB.Buffer.Length) {

                ProcessBuffers(_portInA.Buffer.Length);
            }
        }

        private void ProcessBuffers(int samples) {
            var bufA = _portInA.Read(samples);
            var bufB = _portInB.Read(samples);

            System.Diagnostics.Debug.Assert(bufA.WrittenSamples == bufB.WrittenSamples);

            var samplesA = bufA.GetSamples();
            var samplesB = bufB.GetSamples();
            var samplesC = _outputBuffer.GetSamples();


            for (var i = 0; i < bufA.WrittenSamples; i++) {
                samplesC[i] = samplesA[i] + samplesB[i];
            }

            _outputBuffer.SetWritten(_portInA.Buffer.WrittenSamples);
            _portOut.SendData(_outputBuffer);
        }

        public override string ToString() => Name;

    }

}
