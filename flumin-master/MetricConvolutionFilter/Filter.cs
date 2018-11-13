using System;
using System.ComponentModel;
using System.Globalization;
using System.Xml;
using NodeSystemLib2;
using NodeSystemLib2.Generic;
using NodeSystemLib2.FormatData1D;
using NodeSystemLib2.Generic.NodeAttributes;

namespace MetricConvolutionFilter {

    // TODO: Implement flush and state

    [Metric("Convolution Filter", "Math")]
    public class MetricFilter : StateNode<MetricFilter> {

        private readonly Biquad _bpf = new Biquad(Biquad.BiquadType.Bandpass, 1, 1, 0);

        private readonly InputPortData1D _portInp;
        private readonly OutputPortData1D _portOut;
        private TimeLocatedBuffer1D<double> _outputBuffer;

        private readonly AttributeValueDouble _attrCenterFrequency;
        private readonly AttributeValueDouble _attrQFactor;
        private readonly AttributeValueDouble _attrPeakGainDb;
        private readonly AttributeValueEnum<Biquad.BiquadType> _attrType;

        public MetricFilter(XmlNode node, Graph graph) : this(graph) {
            Deserializing(node);
        }

        public MetricFilter(Graph graph) : base("Filter", graph) {
            _portInp = new InputPortData1D(this, "In");
            _portOut = new OutputPortData1D(this, "Out");

            _portInp.SamplerateChanged += portInp_SamplerateChanged;

            _attrCenterFrequency = new AttributeValueDouble(this, "Center", "Hz");
            _attrPeakGainDb = new AttributeValueDouble(this, "Gain", "dB");
            _attrQFactor = new AttributeValueDouble(this, "Q");
            _attrType = new AttributeValueEnum<Biquad.BiquadType>(this, "Type");

            _attrCenterFrequency.Changed += (s, e) => _bpf.Fc = _portOut.Samplerate > 0 ? _attrCenterFrequency.TypedGet() / _portOut.Samplerate : 0;
            _attrPeakGainDb.Changed += (s, e) => _bpf.PeakGainDb = _attrPeakGainDb.TypedGet();
            _attrQFactor.Changed += (s, e) => _bpf.Q = _attrQFactor.TypedGet();
            _attrType.Changed += (s, e) => _bpf.Type = _attrType.TypedGet();
        }

        private void portInp_SamplerateChanged(object sender, SamplerateChangedEventArgs e) {
            _portOut.Samplerate = _portInp.Samplerate;
            _bpf.Fc = _attrCenterFrequency.TypedGet() / _portInp.Samplerate;
        }

        public override bool CanProcess => _portInp.Available > 0;
        public override bool CanTransfer => _portOut.Buffer.Available > 0;

        public override void PrepareProcessing() {
            _portInp.PrepareProcessing();
            _portOut.PrepareProcessing();

            _outputBuffer = new TimeLocatedBuffer1D<double>(
                DefaultParameters.DefaultBufferMilliseconds.ToSamples(_portInp.Samplerate), 
                _portInp.Samplerate
            );
        }
        
        public override void Process() {
            var buf = _portInp.Read(_outputBuffer.Capacity);

            for (var i = 0; i < buf.Available; i++) {
                _outputBuffer.Data[i] = _bpf.Process(buf.Data[i]);
            }

            _outputBuffer.SetWritten(buf.Available);
            _portOut.Buffer.Write(_outputBuffer);
        }

        public override void Transfer() {
            _portOut.Transfer();
        }

        public override void StartProcessing() { }
        public override void StopProcessing() { }
        public override void SuspendProcessing() { }

    }
}