using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NodeSystemLib2;
using NodeSystemLib2.Generic;
using NodeSystemLib2.Generic.NodeAttributes;
using NodeSystemLib2.FormatData1D;
using System.Xml;

namespace MetricResample
{
    [Metric("Resampler", "Math")]
    public class Resampler : StateNode<Resampler> {

        private LibResampler _resample;
        private TimeLocatedBuffer1D<double> _bufOutput;
        private readonly InputPortData1D _input;
        private readonly OutputPortData1D _output;
        private readonly AttributeValueInt _attrSamplerateOut;

        public int SamplerateOut {
            get { return _attrSamplerateOut.TypedGet(); }
            set {
                _attrSamplerateOut.Set(value);
                _output.Samplerate = value;
            }
        }

        public override bool CanProcess => _input.Available > 0;

        public override bool CanTransfer => _output.Buffer.Available > 0;

        public Resampler(XmlNode node, Graph g) : this(g) {
            Deserializing(node);
        }

        public Resampler(Graph g) : base("Resampler", g) {
            _input = new InputPortData1D(this, "In");
            _output = new OutputPortData1D(this, "Out");

            _attrSamplerateOut = new AttributeValueInt(this, "Output Samplerate", "Hz");
            _attrSamplerateOut.Changed += (s, e) => _output.Samplerate = _attrSamplerateOut.TypedGet();
        }

        public override void PrepareProcessing() {
            _input.PrepareProcessing();
            _output.PrepareProcessing();

            _bufOutput = new TimeLocatedBuffer1D<double>(
                DefaultParameters.DefaultBufferMilliseconds.ToSamples(_output.Samplerate) * 2, // TODO: * 2 pretty much magic
                _output.Samplerate
            );

            _resample = new LibResampler(_input.Samplerate, _output.Samplerate, _input.BufferCapacity, _bufOutput.Capacity);
        }

        public override void Process() {
            var bufIn = _input.Read();
            var cnt = bufIn.Available;

            cnt -= _resample.PutData(bufIn.Data, 0, cnt);
            if (cnt > 0) {
                throw new InvalidOperationException("Resampler could not consume all input samples");
            }
            _resample.Resample();

            if (_resample.OutputAvailable > 0) {
                var samplesToRead = Math.Min(_output.Buffer.Free, _resample.OutputAvailable);
                var actuallyRead = _resample.GetData(_bufOutput.Data, 0, samplesToRead);
                _bufOutput.SetWritten(actuallyRead);

                var written = _output.Buffer.Write(_bufOutput);
                if (written != _bufOutput.Available) {
                    throw new InvalidOperationException("Not all samples could be written to output buffer");
                }
            }
        }

        public override void StartProcessing() { }
        public override void SuspendProcessing() { }

        public override void Dispose() {
            _resample?.Dispose();
            base.Dispose();
        }

        public override void StopProcessing() {
            _resample?.Dispose();
        }

        public override void Transfer() {
            _output.Transfer();
        }
    }
}
