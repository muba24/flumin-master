using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NodeSystemLib2.Generic;
using NodeSystemLib2.Generic.NodeAttributes;
using NodeSystemLib2.FormatData1D;
using NodeSystemLib2.FormatValue;
using NodeEditorLib.EditorControl;
using System.Drawing;
using NodeSystemLib2;
using System.Xml;

namespace Flumin.Metrics {



    [Metric("File", "Other")]
    class MetricFile : StateNode<MetricFile>, INodeUi {

        public enum DataType {
            Float64,
            Float32,
            Int64,
            Int32,
            Int16
        }

        private static Dictionary<DataType, int> _dataTypeSizes = new Dictionary<DataType, int>() {
            { DataType.Float32, sizeof(float) },
            { DataType.Float64, sizeof(double) },
            { DataType.Int16, sizeof(Int16) },
            { DataType.Int32, sizeof(Int32) },
            { DataType.Int64, sizeof(Int64) }
        };

        private readonly OutputPortData1D _portOut;
        private readonly InputPortValueDouble _portTrigger;
        private TimeLocatedBuffer1D<double> _buffer;
        private System.IO.BinaryReader _reader;
        private System.Threading.Timer _timer;
        private TimeStamp? _startTime;

        private int _sampleSize;
        private Func<double> _sampleGetterFunc;

        private readonly AttributeValueFile _attrFilePath;
        private readonly AttributeValueInt _attrSamplerate;
        private readonly AttributeValueEnum<DataType> _attrDataType;

        private const int IntervalTime = 100;

        public MetricFile(Graph g) : base("File Node", g) {
            _portOut = new OutputPortData1D(this, "Out");
            _portTrigger = new InputPortValueDouble(this, "Trig");

            _attrFilePath = new AttributeValueFile(this, "Path", true);
            _attrSamplerate = new AttributeValueInt(this, "Samplerate", 1000);
            _attrDataType = new AttributeValueEnum<DataType>(this, "DataType");

            _attrSamplerate.Changed += (o, e) => _portOut.Samplerate = _attrSamplerate.TypedGet();

            _attrSamplerate.SetRuntimeReadonly();
            _attrDataType.SetRuntimeReadonly();
            _attrFilePath.SetRuntimeReadonly();
        }

        protected override void Serializing(XmlWriter writer) {
            writer.WriteAttributeString(_attrSamplerate.Name, _attrSamplerate.Serialize());
            writer.WriteAttributeString(_attrDataType.Name, _attrDataType.Serialize());
            writer.WriteAttributeString(_attrFilePath.Name, _attrFilePath.Serialize());
            base.Serializing(writer);
        }

        public MetricFile(XmlNode n, Graph g) : this(g) {
            _attrFilePath.Deserialize(n.TryGetAttribute(_attrFilePath.Name, ""));
            _attrSamplerate.Deserialize(n.TryGetAttribute(_attrSamplerate.Name, "1000"));
            _attrDataType.Deserialize(n.TryGetAttribute(_attrDataType.Name, DataType.Float64.ToString()));
        }

        public override bool CanProcess => _portTrigger.Count > 0;
        public override bool CanTransfer => _portOut.Buffer.Available > 0;

        public void OnDoubleClick() {
            if (State != Graph.State.Stopped) return;

            var dialog = new System.Windows.Forms.OpenFileDialog() {
                Filter = "All files (*.*)|*.*",
                CheckFileExists = true,
                CheckPathExists = true,
                Multiselect = false,
            };

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK) {
                _attrFilePath.Set(dialog.FileName);
            }
        }

        public void OnDraw(Rectangle node, Graphics e) {
            //
        }

        public void OnLoad(NodeEditorLib.EditorControl.Node node) {
            //
        }

        public override void PrepareProcessing() {
            _reader?.Dispose();
            try {
                _reader = new System.IO.BinaryReader(System.IO.File.OpenRead(_attrFilePath.TypedGet()));
            } catch (Exception ex) {
                Parent.Context.Notify(new GraphNotification(GraphNotification.NotificationType.Error, ex.ToString()));
                throw;
            }

            if (_reader != null) {
                _reader.BaseStream.Seek(0, System.IO.SeekOrigin.Begin);
                _portOut.PrepareProcessing();
                _portTrigger.PrepareProcessing();
                _buffer = new TimeLocatedBuffer1D<double>(_portOut.Buffer.Capacity, _portOut.Samplerate);
                _endOfStream = false;

                if (_portTrigger.Connection == null) {
                    _startTime = new TimeStamp(0);
                }
            } else {
                throw new Exception("File node: did not specify input");
            }

            _lastStatePosition = 0;

            _sampleSize = _dataTypeSizes[_attrDataType.TypedGet()];

            _sampleGetterFunc = () => {
                throw new System.IO.EndOfStreamException();
            };

            switch (_attrDataType.TypedGet()) {
                case DataType.Float32:
                    _sampleGetterFunc = () => _reader.ReadSingle();
                    break;
                case DataType.Float64:
                    _sampleGetterFunc = () => _reader.ReadDouble();
                    break;
                case DataType.Int16:
                    _sampleGetterFunc = () => _reader.ReadInt16();
                    break;
                case DataType.Int32:
                    _sampleGetterFunc = () => _reader.ReadInt32();
                    break;
                case DataType.Int64:
                    _sampleGetterFunc = () => _reader.ReadInt64();
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        public override void Process() {
            var valueCount = _portTrigger.Count;
            for (int i = 0; i < valueCount; i++) {
                TimeLocatedValue<double> value;
                if (_portTrigger.TryDequeue(out value)) {
                    if (value.Value > 0.5) {
                        _startTime = value.Stamp;
                    }
                } else {
                    break;
                }
            }
        }

        private volatile bool _endOfStream;
        private long _lastStatePosition;

        private void FillBuffer(object arg) {
            if (_endOfStream) return;

            var currentTime = Parent.GetCurrentClockTime();
            var bufferTime = _portOut.Buffer.Time;

            var timeDiff = currentTime.AsSeconds() - bufferTime.AsSeconds();
            if (timeDiff > 0) {
                var samplesToWrite = timeDiff * _portOut.Samplerate;
                var free = (int)Math.Min(_portOut.Buffer.Free, samplesToWrite);
                if (free > 0) {
                    var written = 0;

                    if (!_startTime.HasValue) {
                        for (; written < free; written++) {
                            _buffer.Data[written] = 0;
                        }

                    } else {
                        var timeLeftTillStart = _startTime.Value - bufferTime;
                        var samplesLeftTillStart = Math.Min(free, timeLeftTillStart.ToRate(_portOut.Samplerate));

                        for (; written < samplesLeftTillStart; written++) {
                            _buffer.Data[written] = 0;
                        }

                        try {
                            for (; written < free; written++) {
                                _buffer.Data[written] = _sampleGetterFunc();
                            }
                        } catch (System.IO.EndOfStreamException) {
                            _endOfStream = true;
                        }
                    }

                    _buffer.SetWritten(written);
                    _portOut.Buffer.Write(_buffer);

                    if ((_reader.BaseStream.Position - _lastStatePosition) / _sampleSize >= 10*_attrSamplerate.TypedGet()) {
                        _lastStatePosition = _reader.BaseStream.Position;
                        Parent.Pause();
                        Parent.Flush();

                        Parent.Resume();
                    }

                    if (_endOfStream) {
                        Parent.Context.Notify(new GraphNotification(GraphNotification.NotificationType.Info, "End of file"));
                        Parent.Stop();
                    }
                }
            }
        }

        public override void StartProcessing() {
            _timer = new System.Threading.Timer(FillBuffer, null, 0, IntervalTime);
        }

        public override void StopProcessing() {
            _timer?.Dispose();
        }

        public override void SuspendProcessing() {
            _timer.Change(System.Threading.Timeout.Infinite, System.Threading.Timeout.Infinite);
        }

        public override FlushState FlushData() {
            if (CanTransfer) {
                Transfer();
                return FlushState.Some;
            }
            return FlushState.Empty;
        }

        public override void Transfer() {
            _portOut.Transfer();
        }
    }

}
