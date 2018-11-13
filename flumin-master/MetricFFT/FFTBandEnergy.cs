using System;
using NodeSystemLib2;
using NodeSystemLib2.Generic;
using NodeSystemLib2.Generic.NodeAttributes;
using System.Xml;
using System.ComponentModel;

namespace MetricFFT {

    [Metric("FFT Band Energy", "Math")]
    public class MetricFFTBandEnergy : StateNode<MetricFFTBandEnergy> {

        private readonly NodeSystemLib2.FormatDataFFT.InputPortDataFFT _portInp;
        private readonly NodeSystemLib2.FormatData1D.OutputPortData1D _portOut;

        private readonly AttributeValueDouble _attrCenterFrequency;
        private readonly AttributeValueDouble _attrBandwidth;

        public double CenterFrequency => _attrCenterFrequency.TypedGet();
        public double Bandwidth => _attrBandwidth.TypedGet();

        private double[] _outputBuffer;
        private int _outputIndex;

        public override bool CanProcess => _portInp.Available > 0;
        public override bool CanTransfer => _portOut.Buffer.Available > 0;

        public MetricFFTBandEnergy(XmlNode node, Graph graph) : this(graph) {
            Deserializing(node);
        }
        
        public MetricFFTBandEnergy(Graph graph) : base("Band Energy", graph) {
            _portInp = new NodeSystemLib2.FormatDataFFT.InputPortDataFFT(this, "In");
            _portOut = new NodeSystemLib2.FormatData1D.OutputPortData1D(this, "Out");

            _portInp.SamplerateChanged += (s, e) => _portOut.Samplerate = e.NewSamplerate / _portInp.FFTSize;

            _attrBandwidth = new AttributeValueDouble(this, "Bandwidth", "Hz");
            _attrCenterFrequency = new AttributeValueDouble(this, "Center Frequency", "Hz");
        }

        public override void PrepareProcessing() {
            if (_portInp.FFTSize <= 0) {
                throw new InvalidOperationException("FFT Size must be > 0!");
            }

            _portInp.PrepareProcessing(
                Math.Max(DefaultParameters.MinimumQueueFrameCount, DefaultParameters.DefaultQueueMilliseconds.ToFrames(_portInp.Samplerate, _portInp.FrameSize)),
                Math.Max(DefaultParameters.MinimumBufferFrameCount, DefaultParameters.DefaultBufferMilliseconds.ToFrames(_portInp.Samplerate, _portInp.FrameSize))
            );

            _portOut.PrepareProcessing(
                DefaultParameters.DefaultQueueMilliseconds.ToSamples(_portOut.Samplerate),
                DefaultParameters.DefaultBufferMilliseconds.ToSamples(_portOut.Samplerate)
            );

            _outputIndex = 0;
            _outputBuffer = new double[Math.Max(1, _portOut.Samplerate / 10)];
        }

        public override void Process() {
            Func<double, int> FreqToBin = (f) => (int)(f * _portInp.FFTSize / 2 / _portInp.Samplerate);

            var leftIndex  = FreqToBin(CenterFrequency - Bandwidth / 2);
            var rightIndex = FreqToBin(CenterFrequency + Bandwidth / 2);

            if (leftIndex == rightIndex) rightIndex = leftIndex + 1;

            if (leftIndex < 0) leftIndex = 0;
            else if (leftIndex > _portInp.FFTSize / 2 - 1) leftIndex = _portInp.FFTSize / 2 - 1;

            if (rightIndex < 0) rightIndex = 0;
            else if (rightIndex > _portInp.FFTSize / 2 - 1) rightIndex = _portInp.FFTSize / 2 - 1;

            var buffer = _portInp.Read();

            foreach (var frame in buffer) {
                var sum = 0.0;
                for (int i = leftIndex; i < rightIndex; i++) {
                    sum += frame[i];
                }

                _outputBuffer[_outputIndex++] = sum;

                if (_outputIndex == _outputBuffer.Length) {
                    _portOut.Buffer.Write(_outputBuffer, 0, _outputBuffer.Length);
                    _outputIndex = 0;
                }
            }
        }

        public override void StopProcessing() {}
        public override void StartProcessing() {}
        public override void SuspendProcessing() {}

        public override FlushState FlushData() {
            if (_outputIndex > 0) {
                _portOut.Buffer.Write(_outputBuffer, 0, _outputIndex);
                _outputIndex = 0;
                return FlushState.Some;
            }
            return FlushState.Empty;
        }

        public override void Transfer() {
            _portOut.Transfer();
        }
    }
}
