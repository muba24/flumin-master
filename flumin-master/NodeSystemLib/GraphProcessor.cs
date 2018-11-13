using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace NodeSystemLib {
    public class GraphProcessor {

        class TimeStampTimeRef : ITimeReference<TimeStamp> {
            private Graph g;

            public TimeSpan GetTimeSpan(TimeStamp time) {
                return TimeSpan.FromSeconds((time - g.GetCurrentClockTime()).AsSeconds());
            }

            public TimeStampTimeRef(Graph g) {
                this.g = g;
            }
        }

        class OutputBufferInfo {

            private readonly int[] TargetWrittenTo;

            public OutputPort Source { get; }
            public TimeLocatedBuffer Data { get; private set; }

            private bool _dataDone = true;

            private object _dataLock = new object();

            public void SetBuffer(TimeLocatedBuffer buf) {
                lock (_dataLock) {
                    if (buf == null) throw new InvalidOperationException("Can't be null");
                    Data = buf;
                    for (int i = 0; i < TargetWrittenTo.Length; i++) {
                        TargetWrittenTo[i] = 0;
                    }
                    _dataDone = false;
                }
            }

            public bool IsDone => _dataDone;

            private void CheckForBufferDone() {
                _dataDone = TargetWrittenTo.All(i => i == Data.WrittenSamples);
            }

            public void WriteData() {
                lock (_dataLock) {
                    if (Data == null) return;

                    for (int i = 0; i < TargetWrittenTo.Length; i++) {
                        if (TargetWrittenTo[i] < Data.WrittenSamples) {
                            var tgt = Source.Connections[i];
                            switch (tgt.DataType) {
                                case PortDataType.Array:
                                    var portData = ((DataInputPort)tgt);
                                    var readSignal = portData.RecieveData(Data, TargetWrittenTo[i], Data.WrittenSamples - TargetWrittenTo[i]);
                                    TargetWrittenTo[i] += readSignal;
                                    break;

                                case PortDataType.FFT:
                                    var portFft = ((FFTInputPort)tgt);
                                    var readFft = portFft.RecieveData(Data, TargetWrittenTo[i], Data.WrittenSamples - TargetWrittenTo[i]);
                                    TargetWrittenTo[i] += readFft;
                                    break;

                                default:
                                    throw new InvalidCastException();
                            }
                        }
                    }

                    CheckForBufferDone();
                }
            }

            public OutputBufferInfo(OutputPort source) {
                TargetWrittenTo = new int[source.Connections.Count];
                Source = source;
            }

        }

        private readonly BlockingCollection<Node> _nodeCollection;
        private readonly Thread[] _pool;

        private TimedTaskQueue<TimeStamp> _eventQueue;

        private CancellationTokenSource _cancelSource;

        private Dictionary<OutputPort, OutputBufferInfo> _outputs;

        public Graph Graph { get; }

        public GraphProcessor(Graph g) {
            _pool           = new Thread[Environment.ProcessorCount];
            _nodeCollection = new BlockingCollection<Node>();

            foreach (var node in g.Nodes) {
                _nodeCollection.Add(node);
            }

            Graph = g;
        }

        public bool CanPortWrite(OutputPort port) {
            if (_outputs.ContainsKey(port)) {
                return _outputs[port].IsDone;
            }

            return false;
        }

        public void PostData(OutputPort port, TimeLocatedBuffer buffer) {
            var output = _outputs[port];
            if (!output.IsDone) throw new InvalidOperationException("can't change buffer before sent");
            output.SetBuffer(buffer);
        }

        public void PostEvent(Event ev) {
            _eventQueue.AddAction(ev.Stamp, () => ev.Target.Trigger(ev.Stamp));
        }

        public void Start() {
            _eventQueue = new TimedTaskQueue<TimeStamp>(new TimeStampTimeRef(Graph));

            _outputs = new Dictionary<OutputPort, OutputBufferInfo>();
            foreach (var node in Graph.Nodes) {
                foreach(var output in node.OutputPorts) {
                    if (output.DataType == PortDataType.Array || output.DataType == PortDataType.FFT) {
                        _outputs.Add(output, new OutputBufferInfo(output));
                    }
                }
            }

            _cancelSource = new CancellationTokenSource();
            for (int i = 0; i < _pool.Length; i++) {
                _pool[i] = new Thread(new ParameterizedThreadStart(Worker));
                _pool[i].Start(_cancelSource.Token);
            }
        }

        public void CompleteProcessing() {
            _nodeCollection.CompleteAdding();
        }

        public void Stop() {
            _cancelSource.Cancel();
            for (int i = 0; i < _pool.Length; i++) {
                _pool[i].Join();
            }
            _eventQueue.CancelAll();
            _eventQueue.Dispose();
        }

        // There needs to be a smarter way of managing the jobs.
        // For example, a node can drop out of the collection.
        // But when a specific event occurs, it is taken back into the collection.

        private void Worker(object argument) {
            var cancel = (CancellationToken)argument;

            try {
                foreach (var node in _nodeCollection.GetConsumingEnumerable(cancel)) {
                    var processed = false;

                    // check if has packet in output currently
                    foreach (var output in node.OutputPorts) {
                        if (_outputs.ContainsKey(output)) {
                            var outInfo = _outputs[output];
                            if (!outInfo.IsDone) {
                                outInfo.WriteData();
                                processed = true;
                            }
                        }
                    }

                    if (!processed) {
                        foreach (var input in node.InputPorts) {
                            if (input.DataType == PortDataType.Array) {
                                var portData = (DataInputPort)input;
                                var queueLen = portData.Queue?.Length ?? 0;
                                if (queueLen > 0) {
                                    node.TriggerProcessing(portData);
                                    if (portData.Queue.Length < queueLen) {
                                        processed = true;
                                    }
                                }

                            } else if (input.DataType == PortDataType.FFT) {
                                var portDataFft = (FFTInputPort)input;
                                var queueLen = portDataFft.Queue?.Length ?? 0;
                                if (queueLen > 0) {
                                    node.TriggerProcessing(portDataFft);
                                    if (portDataFft.Queue.Length < queueLen) {
                                        processed = true;
                                    }
                                }

                            } else if (input.DataType == PortDataType.Value) {
                                var portValue = (ValueInputPort)input;
                                if ((portValue.Values?.Count ?? 0) > 0) {
                                    node.TriggerProcessing(portValue);
                                    processed = true;
                                }
                            }
                        }
                    }

                    try {
                        _nodeCollection.Add(node);
                    } catch (InvalidOperationException) {
                        // CompleteProcessing called, workers should run dry
                    }

                    if (!processed) {
                        Thread.Sleep(1);
                    }
                }
            } catch (OperationCanceledException) {
                return;
            }
        }

    }
}
