using NodeSystemLib2;
using NodeSystemLib2.FormatData1D;
using NodeSystemLib2.FormatValue;
using NodeSystemLib2.Generic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Flumin.Metrics {

    [Metric("Hystersis Threshold", "Values")]
    class MetricThresholdHystersis : StateNode<MetricThresholdHystersis> {

        private enum TristateActive {
            Inactive,
            Active
        }

        private TristateActive _active;

        private readonly InputPortData1D _portIn;
        private readonly InputPortValueDouble _portThreshLow;
        private readonly InputPortValueDouble _portThreshHigh;
        private readonly OutputPortValueDouble _portOut;

        public MetricThresholdHystersis(XmlNode n, Graph g) : this(g) {
        }

        public MetricThresholdHystersis(Graph g) : base("Hysteresis Threshold", g) {
            _portIn = new InputPortData1D(this, "In");
            _portThreshHigh = new InputPortValueDouble(this, "High");
            _portThreshLow = new InputPortValueDouble(this, "Low");
            _portOut = new OutputPortValueDouble(this, "Out");
        }

        public override bool CanProcess => _portIn.Available > 0;
        public override bool CanTransfer => _portOut.BufferedValueCount > 0;

        public override void PrepareProcessing() {
            _active = TristateActive.Inactive;
            _portIn.PrepareProcessing();
            _portThreshHigh.PrepareProcessing();
            _portThreshLow.PrepareProcessing();
            _portOut.PrepareProcessing();
        }

        public override void StartProcessing() { }
        public override void StopProcessing() { }
        public override void SuspendProcessing() { }

        public override void Process() {
            foreach (var tuple in _portIn.Read().ZipWithValueInput(_portThreshLow, _portThreshHigh)) {
                switch (_active) {
                    case TristateActive.Active:
                        if (tuple.Sample < tuple.Scalar && tuple.Scalar < tuple.Scalar2) {
                            _active = TristateActive.Inactive;
                            _portOut.BufferForTransfer(new TimeLocatedValue<double>(0, tuple.Stamp));
                        }
                        break;
                    case TristateActive.Inactive:
                        if (tuple.Sample > tuple.Scalar2 && tuple.Scalar < tuple.Scalar2) {
                            _active = TristateActive.Active;
                            _portOut.BufferForTransfer(new TimeLocatedValue<double>(1, tuple.Stamp));
                        }
                        break;
                }
            }
        }

        public override void Transfer() {
            _portOut.TransferBuffer();
        }
    }
}
