using System;
using System.Xml;
using NodeSystemLib2;
using NodeSystemLib2.Generic;
using NodeSystemLib2.FormatData1D;
using NodeSystemLib2.FormatValue;

namespace SimpleADC.Metrics {

    [Metric("Threshold", "Values")]
    public class MetricThreshold : StateNode<MetricThreshold> {

        private InputPortData1D _portInp;
        private InputPortValueDouble _portThresh;
        private OutputPortValueDouble _portOut;

        private TimeLocatedValue<double> _input;
        private TimeLocatedValue<double> _threshold;
        private TimeLocatedValue<double> _output;

        public MetricThreshold(XmlNode node, Graph graph) : this(graph) { }

        public MetricThreshold(Graph graph) : base("Threshold", graph) {
            _portInp    = new InputPortData1D(this, "In");
            _portThresh = new InputPortValueDouble(this, "Threshold");
            _portOut    = new OutputPortValueDouble(this, "Out");

            _output     = new TimeLocatedValue<double>(0, TimeStamp.Zero);
        }

        public override string ToString() => Name;

        object _valueLock = new object();

        protected override void ValueAvailable(ValueInputPort port) {
            lock (_valueLock) {
                while (_portInp.Values.Count > 0) {
                    _portInp.Values.TryDequeue(out _input);
                }
            }

            while (_portThresh.Values.Count > 0) {
                _portThresh.Values.TryDequeue(out _threshold);
            }

            lock (_valueLock) {
                if (_input != null && _threshold != null) {
                    Process();
                }
            }
        }

        private void Process() {
            if (_output.Value < 0.5) {
                if (_input.Value > _threshold.Value) {
                    _output = new TimeLocatedValue(1.0, Parent.GetCurrentClockTime());
                    _portOut.SendData(_output);
                }
            } else {
                if (_input.Value < _threshold.Value) {
                    _output = new TimeLocatedValue(0.0, Parent.GetCurrentClockTime());
                    _portOut.SendData(_output);
                }
            }
        }

    }
}