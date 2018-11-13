using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NodeSystemLib;
using System.Xml;

namespace SimpleADC.Metrics {

    [Metric("FFT Band Energy", "Math")]
    public class MetricFFTBandEnergy : Node {

        private TimeLocatedBuffer   _buffer;
        private FFTInputPort        _portInp;
        private ValueOutputPort     _portOut;

        public double               CenterFrequency { get; set; }
        public double               Bandwidth       { get; set; }


        public MetricFFTBandEnergy(XmlNode node, Graph graph) : this(graph) { }


        public MetricFFTBandEnergy(Graph graph)
            : base("Band Energy", graph,
                  InputPort.CreateMany(
                      InputPort.Create("in", PortDataType.FFT)),
                  OutputPort.CreateMany(
                      OutputPort.Create("out", PortDataType.Value))) {

            _portInp = (FFTInputPort)InputPorts[0];
            _portOut = (ValueOutputPort)OutputPorts[0];
        }


        public override string ToString() => Name;


        public override bool PrepareProcessing() {
            if (_portInp.FFTSize <= 0) {
                GlobalSettings.Instance.Errors.Add(new Error("FFT Size must be > 0!"));
                return false;
            }
            _portInp.InitBuffer();
            return true;
        }

        // TODO: REIHENFOLGE, IN DER FFTSIZECHANGED UND INPUTSAMPLERATECHANGED AUFGERUFEN WERDEN
        protected override void FFTSizeChanged(InputPort e) {
            _buffer = new TimeLocatedBuffer(_portInp.FFTSize / 2, _portInp.Samplerate);
        }

        protected override void InputSamplerateChanged(InputPort e) {
            if (_buffer != null) {
                _buffer.Samplerate = _portInp.Samplerate;
            } else {
                if (_portInp.FFTSize > 0) {
                    _buffer = new TimeLocatedBuffer(_portInp.FFTSize / 2, _portInp.Samplerate);
                }
            }
        }

        protected override void FftDataAvailable(FFTInputPort port) {
            Func<double, int> FreqToBin = (f) => (int)(f * _portInp.FFTSize / _buffer.Samplerate);

            var leftIndex  = FreqToBin(CenterFrequency - Bandwidth / 2) + 1;
            var rightIndex = FreqToBin(CenterFrequency + Bandwidth / 2);

            if (leftIndex == rightIndex) rightIndex = leftIndex + 1;

            if (leftIndex < 0) leftIndex = 0;
            else if (leftIndex > _portInp.FFTSize / 2 - 1) leftIndex = _portInp.FFTSize / 2 - 1;
             
            if (rightIndex < 0) rightIndex = 0;
            else if (rightIndex > _portInp.FFTSize / 2 - 1) rightIndex = _portInp.FFTSize / 2 - 1;

            while (_portInp.Queue.Length > _buffer.Length) {
                _portInp.Queue.Dequeue(_buffer);

                var samples    = _buffer.GetSamples();

                var sum = 0.0;
                for (int i = leftIndex; i < rightIndex; i++)
                    sum += samples[i];

                _portOut.SendData(new TimeLocatedValue(sum, TimeStamp.Zero()));
            }
        }

    }
}
