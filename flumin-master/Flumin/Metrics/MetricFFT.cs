using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NodeSystemLib;
using System.Xml;
using FFTW;

namespace SimpleADC.Metrics {

    [Metric("FFT", "Math")]
    public class MetricFFT : Node {

        FFTWTransform _fft;
        TimeLocatedBuffer _bufIn;
        TimeLocatedBuffer _bufOut;

        DataInputPort _portInp;
        FFTOutputPort _portOut;

        int _fftSize;

        public int FFTSize
        {
            get
            {
                return _fftSize;
            }

            set
            {
                _fftSize = value;
                _portOut.FFTSize = _fftSize;
                if (_fft != null) _fft.Dispose();
                _fft = new FFTWTransform(_portOut.FFTSize);
                InitBuffers();
            }
        }

        public MetricFFT(XmlNode node, Graph graph) : this(graph) { }

        public MetricFFT(Graph graph)
            : base("FFT", graph,
                  InputPort.CreateMany(
                      InputPort.Create("in", PortDataType.Array)),
                  OutputPort.CreateMany(
                      OutputPort.Create("out", PortDataType.FFT))) {

            _portInp = (DataInputPort)InputPorts[0];
            _portOut = (FFTOutputPort)OutputPorts[0];
            FFTSize = 2048;
        }

        public override string ToString() => Name;

        private void InitBuffers() {
            if (_portInp.Samplerate != 0) {
                var ffts = (int)((GlobalSettings.Instance.BufferSizeMilliseconds / 1000.0) * (_portInp.Samplerate / _fft.Size));
                _bufIn = new TimeLocatedBuffer(_fft.Size * ffts, _portInp.Samplerate);
                _bufOut = new TimeLocatedBuffer(_fft.Size / 2 * ffts, _portInp.Samplerate / 2);
                _portInp.InitBuffer();
                _portInp.Queue.SizeFixed = true;
            }
        }

        protected override void InputSamplerateChanged(InputPort e) {
            InitBuffers();
            _portOut.Samplerate = _portInp.Samplerate / 2;
        }

        protected override void DataAvailable(DataInputPort port) {
            while (_portInp.Queue.Length > _bufIn.Length) {
                var samples = _bufOut.GetSamples();
                var k       = 0;

                _portInp.Queue.Dequeue(_bufIn);

                for (int i = 0; i < _bufIn.Length; i += _fft.Size) {
                    _fft.UseData(_bufIn.GetSamples(), i, _fft.Size);
                    var result = _fft.Transform();

                    for (int j = 0; j < result.Length; j++)
                        samples[k++] = result[j];
                }

                _bufOut.SetWritten(k);
                _portOut.SendData(_bufOut);
            }
        }

    }
}
