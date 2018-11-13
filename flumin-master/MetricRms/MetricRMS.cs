using NodeSystemLib2;
using NodeSystemLib2.Generic;
using NodeSystemLib2.Generic.NodeAttributes;
using NodeSystemLib2.FormatData1D;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace MetricRms
{
    [Metric("RMS", "Math")]
    public class MetricRms : StateNode<MetricRms> {

        private readonly OutputPortData1D _portOut;
        private readonly InputPortData1D _portIn;
        private readonly AttributeValueDouble _attrMilliseconds;

        private TimeLocatedBuffer1D<double> _bufOut;

        private int _samplesPerRms;
        private int _inputSampleCounter;
        private int _outputSampleCounter;

        private double _sum;

        public override bool CanProcess => _portIn.Available > 0;
        public override bool CanTransfer => _portOut.Buffer.Available > 0;

        public MetricRms(XmlNode node, Graph g) : this(g) {
            Deserializing(node);
        }

        public override string ToString() => Name;

        public MetricRms(Graph g) : base("RMS", g) {
            _portIn = new InputPortData1D(this, "In");
            _portOut = new OutputPortData1D(this, "Out");

            _portIn.SamplerateChanged += (s, e) => UpdateSamplerate();

            _attrMilliseconds = new AttributeValueDouble(this, "Window Length", "ms");
            _attrMilliseconds.Changed += (s, e) => UpdateSamplerate();
            _attrMilliseconds.SetRuntimeReadonly();
        }

        private void UpdateSamplerate() {
            if (_portIn.Samplerate > 0) {
                _samplesPerRms = (int)(_attrMilliseconds.TypedGet() * _portIn.Samplerate / 1000);
                if (_samplesPerRms > 2) {
                    _portOut.Samplerate = _portIn.Samplerate / _samplesPerRms;
                } else {
                    _portOut.Samplerate = 0;
                }
            }
        }

        public override void PrepareProcessing() {
            if (_portIn.Samplerate == 0) {
                throw new InvalidOperationException("Input samplerate must be > 0");
            }

            if (_samplesPerRms <= 0) {
                throw new InvalidOperationException("Window length too short. Less than 1 sample.");
            }

            if (_portOut.Samplerate == 0) {
                throw new InvalidOperationException("Output samplerate is to small. Must be > 0. Chose a shorter window length");
            }

            _portIn.PrepareProcessing();
            _portOut.PrepareProcessing();

            _bufOut = new TimeLocatedBuffer1D<double>(DefaultParameters.DefaultBufferMilliseconds.ToSamples(_portOut.Samplerate), _portOut.Samplerate);
        }

        public override void StopProcessing() {
            _inputSampleCounter = 0;
            _outputSampleCounter = 0;
            _sum = 0;
        }

        public override void Process() {
            var buffer = _portIn.Read();
            var samples = buffer.Data;

            for (int i = 0; i < buffer.Available; i++) {
                _sum += samples[i] * samples[i];
                if (++_inputSampleCounter >= _samplesPerRms) {
                    _bufOut.Data[_outputSampleCounter++] = Math.Sqrt(_sum / _samplesPerRms);
                    _inputSampleCounter = 0;
                    _sum = 0;

                    if (_outputSampleCounter == _bufOut.Capacity) {
                        _bufOut.SetWritten(_outputSampleCounter);
                        _portOut.Buffer.Write(_bufOut);
                        _outputSampleCounter = 0;
                    }
                }
            }
        }

        public override void StartProcessing() {}
        public override void SuspendProcessing() {}

        public override void Transfer() {
            _portOut.Transfer();
        }
    }
}
