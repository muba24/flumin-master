using NodeSystemLib2;
using NodeSystemLib2.Generic.NodeAttributes;
using NodeSystemLib2.Generic;
using NodeSystemLib2.FormatData1D;
using NodeSystemLib2.FormatValue;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Flumin.Metrics {

    [Metric("Sustained Threshold", "Values")]
    class MetricSustainedThreshold : StateNode<MetricSustainedThreshold> {
        private readonly InputPortData1D _portIn;
        private readonly InputPortValueDouble _portThresh;
        private readonly OutputPortValueDouble _portOut;

        public enum SustainMode {
            StartAtRisingEdge,
            StartAtFallingEdge
        }

        private enum TristateActive {
            Inactive,
            BecomingInactive,
            Active
        }

        private TristateActive _active;
        private TimeStamp _timeBecomeInactive;

        private AttributeValueDouble _attrActiveDurationMillis;
        private AttributeValueEnum<SustainMode> _attrStartAt;

        public override bool CanProcess => _portIn.Available > 0;
        public override bool CanTransfer => _portOut.BufferedValueCount > 0;

        public MetricSustainedThreshold(XmlNode n, Graph g) : this(g) {
            _attrActiveDurationMillis.Deserialize(n.TryGetAttribute(_attrActiveDurationMillis.Name, "0"));
            _attrStartAt.Deserialize(n.TryGetAttribute(_attrStartAt.Name, SustainMode.StartAtFallingEdge.ToString()));
        }

        public MetricSustainedThreshold(Graph g) : base("Sustained Threshold", g) {
            _portIn     = new InputPortData1D(this, "In");
            _portThresh = new InputPortValueDouble(this, "Thresh");
            _portOut    = new OutputPortValueDouble(this, "Out");
            _attrActiveDurationMillis = new AttributeValueDouble(this, "SustainDuration", "ms", 1000);
            _attrStartAt = new AttributeValueEnum<SustainMode>(this, "SustainStart");
        }

        protected override void Serializing(XmlWriter writer) {
            writer.WriteAttributeString(_attrActiveDurationMillis.Name, _attrActiveDurationMillis.Serialize());
            writer.WriteAttributeString(_attrStartAt.Name, _attrStartAt.Serialize());
            base.Serializing(writer);
        }

        public override void PrepareProcessing() {
            _active = TristateActive.Inactive;
            _timeBecomeInactive = TimeStamp.Zero;
            _portIn.PrepareProcessing();
            _portThresh.PrepareProcessing();
            _portOut.PrepareProcessing();
        }

        public override void StartProcessing() { }
        public override void StopProcessing() { }
        public override void SuspendProcessing() { }

        public override void Process() {
            if (_attrStartAt.TypedGet() == SustainMode.StartAtFallingEdge) {
                foreach (var tuple in _portIn.Read().ZipWithValueInput(_portThresh)) {
                    if (tuple.Sample >= tuple.Scalar) {
                        _timeBecomeInactive = tuple.Stamp.Increment(_attrActiveDurationMillis.TypedGet() / 1000.0);
                        if (_active == TristateActive.Inactive) {
                            _active = TristateActive.Active;
                            _portOut.BufferForTransfer(new TimeLocatedValue<double>(1, tuple.Stamp));
                        }
                    } else {
                        if (_active == TristateActive.Active && _timeBecomeInactive <= tuple.Stamp) {
                            _active = TristateActive.Inactive;
                            _portOut.BufferForTransfer(new TimeLocatedValue<double>(0, tuple.Stamp));
                        }
                    }
                }
            } else if (_attrStartAt.TypedGet() == SustainMode.StartAtRisingEdge) {
                foreach (var tuple in _portIn.Read().ZipWithValueInput(_portThresh)) {
                    if (tuple.Sample >= tuple.Scalar) {
                        if (_active == TristateActive.Inactive) {
                            _timeBecomeInactive = tuple.Stamp.Increment(_attrActiveDurationMillis.TypedGet() / 1000.0);
                            _active = TristateActive.Active;
                            _portOut.BufferForTransfer(new TimeLocatedValue<double>(1, tuple.Stamp));
                        }
                    } else {
                        if (_active == TristateActive.BecomingInactive) {
                            _active = TristateActive.Inactive;
                        }
                    }

                    if (_active == TristateActive.Active && _timeBecomeInactive <= tuple.Stamp) {
                        _active = TristateActive.BecomingInactive;
                        _portOut.BufferForTransfer(new TimeLocatedValue<double>(0, tuple.Stamp));
                    }
                }
            }
        }

        public override void Transfer() {
            _portOut.TransferBuffer();
        }
    }
}

