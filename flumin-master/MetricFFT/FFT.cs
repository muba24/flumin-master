using NodeSystemLib2;
using NodeSystemLib2.Generic;
using System.Xml;
using FFTW;
using System;

namespace MetricFFT {

    [Metric("FFT", "Math")]
    public class MetricFFT : StateNode<MetricFFT> {


        readonly NodeSystemLib2.FormatData1D.InputPortData1D _portInp;
        readonly NodeSystemLib2.FormatDataFFT.OutputPortDataFFT _portOut;
        readonly NodeSystemLib2.Generic.NodeAttributes.AttributeValueInt _attrFFTSize;

        FFTWTransform _fft;
        int inputSampleCount;
        double[] _frameBuffer;

        public int FFTSize => _attrFFTSize.TypedGet();

        public override bool CanProcess => _portInp.Available > 0;

        public override bool CanTransfer => _portOut.FramesAvailable > 0;

        public MetricFFT(XmlNode node, Graph graph) : this(graph) {
            Deserializing(node);
        }

        public MetricFFT(Graph graph) : base("FFT", graph) {
            _portOut = new NodeSystemLib2.FormatDataFFT.OutputPortDataFFT(this, "out");
            _portInp = new NodeSystemLib2.FormatData1D.InputPortData1D(this, "in");

            _portInp.SamplerateChanged += (s, e) => _portOut.Samplerate = _portInp.Samplerate;

            _attrFFTSize = new NodeSystemLib2.Generic.NodeAttributes.AttributeValueInt(this, "FFT Size", "Samples",
                (x) => x < 2 ? 2 : (int)Math.Pow(2, Math.Round(Math.Log(x, 2)))
            );
            _attrFFTSize.SetRuntimeReadonly();
            _attrFFTSize.Changed += (s, e) => _portOut.FFTSize = _attrFFTSize.TypedGet();
            _attrFFTSize.Set(512);
        }

        private void InitBuffers() {
            _portInp.PrepareProcessing(
                Math.Max(DefaultParameters.MinimumQueueFrameCount * FFTSize, DefaultParameters.DefaultQueueMilliseconds.ToSamples(_portInp.Samplerate)),
                Math.Max(FFTSize, DefaultParameters.DefaultBufferMilliseconds.ToSamples(_portOut.Samplerate))
            );

            _portOut.PrepareProcessing(
                Math.Max(DefaultParameters.MinimumQueueFrameCount, DefaultParameters.DefaultQueueMilliseconds.ToFrames(_portInp.Samplerate, _portOut.FrameSize)),
                Math.Max(DefaultParameters.MinimumBufferFrameCount, DefaultParameters.DefaultBufferMilliseconds.ToFrames(_portInp.Samplerate, _portOut.FrameSize))
            );

            _frameBuffer = new double[FFTSize];
        }

        public override void PrepareProcessing() {
            if (_portInp.Samplerate <= 0) {
                throw new InvalidOperationException("Input Samplerate must be > 0 Hz");
            }

            if (_fft == null || _fft.Size != FFTSize) {
                _fft?.Dispose();
                _fft = new FFTWTransform(_portOut.FFTSize);
            }

            inputSampleCount = 0;
            InitBuffers();
        }

        public override void Process() {
            var fftsToRead = Math.Min(_portInp.BufferCapacity / FFTSize, _portInp.Available / FFTSize);
            var outputFramesFree = _portOut.Free;
            if (outputFramesFree == 0) return;

            var inputBuffer   = _portInp.Read(Math.Min(outputFramesFree, fftsToRead) * FFTSize);
            var inputSamples  = inputBuffer.Data;

            for (int i = 0; i < inputBuffer.Available; i++) {
                _frameBuffer[inputSampleCount++] = inputSamples[i];

                if (inputSampleCount == _fft.Size) {
                    _fft.UseData(_frameBuffer, 0, _fft.Size);
                    var result = _fft.Transform();
                    _portOut.WriteFrame(result, 0);
                    inputSampleCount = 0;
                }
            }
        }

        public override void StopProcessing() {}
        public override void StartProcessing() {}
        public override void SuspendProcessing() {}

        public override void Transfer() {
            _portOut.Transfer();
        }
    }
}
