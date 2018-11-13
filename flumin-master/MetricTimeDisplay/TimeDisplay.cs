using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NodeSystemLib2;
using NodeSystemLib2.FormatData1D;
using NodeSystemLib2.FormatValue;
using NodeSystemLib2.Generic;
using NodeSystemLib2.Generic.NodeAttributes;
using System.Xml;
using System.Drawing;
using System.ComponentModel;

namespace MetricTimeDisplay
{
    [Metric("Display 2", "Display")]
    public class MetricDisplay2 : StateNode<MetricDisplay2>, INodeUi {

        public interface IChannel : ICloneable { }

        public class ValueChannel : IChannel {
            public InputPortValueDouble Port;
            public Queue<TimeLocatedValue<double>> Data;

            public object Clone() {
                return null;
                //    var channel  = new ValueChannel();
                //    channel.Port = Port;
                //    channel.Data = new Queue<TimeLocatedValue<double>>(Data);

                //    return channel;
            }
        }

        public class DataChannel : IChannel {
            public InputPortData1D Port;
            public RingBuffer1D<double> Data;
            public TimeLocatedBuffer1D<double> PortBuffer;
            public TimeLocatedBuffer1D<double> DisplayBuffer;

            public void LoadFromChannel(DataChannel channel) {
                throw new NotImplementedException();

                //Port = channel.Port;
                //Data.LoadState(channel.Data);
                //PortBuffer.LoadState(channel.PortBuffer);
                //DisplayBuffer.LoadState(channel.DisplayBuffer);
            }

            public object Clone() {
                return null;
                //    var channel           = new DataChannel();
                //    channel.Port          = Port;
                //    channel.Data          = Data.Clone();
                //    channel.PortBuffer    = (TimeLocatedBuffer)PortBuffer.Clone();
                //    channel.DisplayBuffer = new NodeSystemLib2.FormatData1D.TimeLocatedBuffer1D<double>(DisplayBuffer.Capacity, Port.Samplerate);

                //    Array.Copy(channel.DisplayBuffer.Data, DisplayBuffer.Data, DisplayBuffer.Available);

                //    return channel;
            }
        }

        private readonly Dictionary<InputPort, IChannel> _channels
            = new Dictionary<InputPort, IChannel>();

        private readonly AttributeValueDouble _attrLookAheadFactor;
        private readonly AttributeValueInt _attrWindowLength;

        private Display2Window _wnd;

        private readonly object _wndLock = new object();
        private readonly object _processingLock = new object();
        
        public Dictionary<InputPort, IChannel> Channels => _channels;

        public override bool CanProcess => InputPorts.OfType<InputPortData1D>().Any(p => p.Available > 0) ||
                                           InputPorts.OfType<InputPortValueDouble>().Any(p => p.Count > 0);

        public override bool CanTransfer => false;

        public double LookAheadFactor => _attrLookAheadFactor.TypedGet();
        public int Milliseconds => _attrWindowLength.TypedGet();

        public MetricDisplay2(XmlNode node, Graph g) : this(g) {
            Deserializing(node);
        }

        public MetricDisplay2(Graph g) : base("Display", g) {
            var dataInp = new InputPortData1D(this, "Dinp 1");
            var valueInp = new InputPortValueDouble(this, "Vinp 1");

            _attrLookAheadFactor = new AttributeValueDouble(this, "Look Ahead Factor", 1);
            _attrWindowLength = new AttributeValueInt(this, "Window Length", "ms", 1000);
            _attrLookAheadFactor.SetRuntimeReadonly();

            _attrWindowLength.Changed += (s, e) => {
                lock (_processingLock) {
                    lock (_wndLock) {
                        if (State == Graph.State.Running) {
                            CreateChannels();
                            _wnd?.PrepareProcessing();
                        }
                    }
                }
            };

            PortConnectionChanged += MetricDisplay2_PortConnectionChanged;

            try {
                CreateWindow();
            } catch (Exception e) {
                throw new InvalidOperationException("Error in constructor while creating window: " + e);
            }
        }

        private void MetricDisplay2_PortConnectionChanged(object sender, ConnectionModifiedEventArgs e) {
            //var port = (Port)sender;
            //if (port.DataType.Equals(PortDataTypes.TypeIdSignal1D)) {
            //    var dataInputPorts = InputPorts.OfType<InputPortData1D>();
            //    if (e.Connection != null) {
            //        var newPort = new InputPortData1D(this, $"Dinp {dataInputPorts.Count() + 1}");
            //    } else {
            //        if (dataInputPorts.Count() > 1) RemoveLastUnusedInputPortOf<InputPortData1D>();
            //    }

            //} else if (port.DataType.Equals(PortDataTypes.TypeIdValueDouble)) {
            //    var valueInputPorts = InputPorts.OfType<InputPortValueDouble>();
            //    if ((e.Action == ConnectionModifiedEventArgs.Modifier.Added || 
            //        e.Action == ConnectionModifiedEventArgs.Modifier.Changed) && 
            //        e.Connection != null) {

            //        var newPort = new InputPortValueDouble(this, $"Vinp {valueInputPorts.Count() + 1}");
            //    } else {
            //        if (valueInputPorts.Count() > 1) RemoveLastUnusedInputPortOf<InputPortValueDouble>();
            //    }
            //}
        }

        private void RemoveLastUnusedInputPortOf<T>() where T : InputPort {
            foreach (var p in InputPorts.OfType<T>().Reverse()) {
                if (p.Connection == null) {
                    RemovePort(p);
                    break;
                }
            }
        }

        public int ReadFromDataPort(InputPortData1D port, TimeLocatedBuffer1D<double> output) {
            lock (_processingLock) {
                var ringbuffer = ((DataChannel)_channels[port]).Data;
                var read = ringbuffer.Peek(output, output.Capacity);
                return read;
            }
        }

        //protected override void ValueAvailable(InputPortValueDouble port) {
        //    lock (_wndLock) {
        //        lock (_processingLock) {
        //            foreach (var inp in InputPorts.OfType<InputPortValueDouble>().Where(i => i.Connection != null)) {
        //                var channel = ((ValueChannel)_channels[inp]);
        //                TimeLocatedValue a;
        //                while(inp.Values.TryDequeue(out a)) {
        //                    if (_wnd != null) {
        //                        channel.Data.Enqueue(a);
        //                    }
        //                }
        //            }
        //        }
        //    }
        //}

        public override void Process() {
            lock (_wndLock) {
                lock (_processingLock) {

                    foreach (var inp in InputPorts.OfType<InputPortData1D>()) {
                        if (_channels.ContainsKey(inp)) {
                            var channel = ((DataChannel)_channels[inp]);
                            var frames = inp.Read();
                            if (_wnd != null) {
                                channel.Data.Write(frames, 0, frames.Available);
                            }
                        }
                    }

                    foreach (var inp in InputPorts.OfType<InputPortValueDouble>()) {
                        if (_channels.ContainsKey(inp)) {
                            var channel = ((ValueChannel)_channels[inp]);
                            for (int i = 0; i < inp.Count; i++) {
                                TimeLocatedValue<double> value;
                                if (inp.TryDequeue(out value)) {
                                    if (_wnd != null) {
                                        channel.Data.Enqueue(value);
                                    }
                                }
                            }
                        }
                    }

                }
            }
        }

        public override void StopProcessing() {
            lock (_wndLock) {
                if (_wnd != null && !_wnd.IsDisposed) {
                    _wnd.Run = false;
                    _wnd.StopProcessing();
                }
            }
        }

        public override void PrepareProcessing() {
            if (_attrLookAheadFactor.TypedGet() <= 0) {
                throw new ArgumentOutOfRangeException(_attrLookAheadFactor.Name, "Lookahead Factor must be > 0");
            }

            if (_attrWindowLength.TypedGet() <= 0) {
                throw new ArgumentOutOfRangeException(_attrWindowLength.Name, "Window length must be > 0 ms");
            }

            CreateChannels();

            lock (_wndLock) {
                lock (_processingLock) {
                    if (_wnd == null || _wnd.IsDisposed) {
                        try {
                            CreateWindow();
                        } catch (Exception e) {
                            throw new InvalidOperationException("Error while creating window: " + e);
                        }
                    }

                    _wnd.Run = true;
                    if (_wnd.IsLoaded) {
                        _wnd.PrepareProcessing();
                    }
                }
            }
        }

        private void CreateChannels() {
            var maxBufLen = 0;

            lock (_wndLock) {
                _wnd?.ClearChannels();
            }

            foreach (var portInp in InputPorts.OfType<InputPortData1D>()) {
                if (portInp.Samplerate > 0) {
                    portInp.PrepareProcessing(
                        DefaultParameters.DefaultQueueMilliseconds.ToSamples(portInp.Samplerate),
                        DefaultParameters.DefaultBufferMilliseconds.ToSamples(portInp.Samplerate)
                    );

                    var samples = (int)(_attrLookAheadFactor.TypedGet() * _attrWindowLength.TypedGet() * (long)portInp.Samplerate / 1000L);
                    var buffer  = new TimeLocatedBuffer1D<double>(DefaultParameters.DefaultBufferMilliseconds.ToSamples(portInp.Samplerate), portInp.Samplerate);
                    var timeBuf = new RingBuffer1D<double>(samples, portInp.Samplerate) { Overflow = true };

                    maxBufLen = Math.Max(maxBufLen, buffer.Capacity);

                    var channel = new DataChannel {
                        Port          = portInp,
                        Data          = timeBuf,
                        PortBuffer    = buffer,
                        DisplayBuffer = new TimeLocatedBuffer1D<double>(samples, portInp.Samplerate)
                    };

                    if (_channels.ContainsKey(portInp)) _channels[portInp] = channel;
                    else _channels.Add(portInp, channel);
                }
            }

            foreach (var port in InputPorts.OfType<InputPortValueDouble>()) {
                if (port.Connection != null) {
                    var channel = new ValueChannel {
                        Port = port,
                        Data = new Queue<TimeLocatedValue<double>>(2048)
                    };

                    if (_channels.ContainsKey(port)) _channels[port] = channel;
                    else _channels.Add(port, channel);
                }
            }
        }

        public void OnLoad(NodeEditorLib.EditorControl.Node node) {
            //
        }

        //public override NodeState SaveState() {
        //    var state = NodeState.Save(this, Parent.GetCurrentClockTime());

        //    foreach (var port in _channels.Keys) {
        //        state["port" + port.Name] = _channels[port].Clone();
        //    }

        //    return state;
        //}

        //public override void LoadState(NodeState state) {
        //    System.Diagnostics.Debug.Assert(state.Parent == this);
        //    state.Load();

        //    // ToArray as the collection will be modified inside the loop -> copy enumeration
        //    // TODO: Add ValueChannel support
        //    foreach (var port in _channels.Keys.ToArray()) {
        //        ((DataChannel)_channels[port]).LoadFromChannel((DataChannel)state["port" + port.Name]);
        //    }
        //}

        public void OnDoubleClick() {
            lock (_wndLock) {
                if (_wnd == null || _wnd.IsDisposed) {
                    try {
                        CreateWindow();
                    } catch (Exception e) {
                        throw new InvalidOperationException("Could not load window");
                        return;
                    }
                }

                _wnd.Show(
                    (WeifenLuo.WinFormsUI.Docking.DockPanel)Parent.Context.DockPanel,
                    WeifenLuo.WinFormsUI.Docking.DockState.DockRight
                );

                _wnd.SetXAxis(new TimeStamp(0), new TimeStamp(_attrWindowLength.TypedGet() / 1000.0));

                if (State == Graph.State.Running) {
                    _wnd.Run = true;
                    _wnd.PrepareProcessing();
                }
            }
        }

        /// <summary>
        /// Creates a new window. Disposes the old one, if it exists and isn't disposed.
        /// </summary>
        private void CreateWindow() {
            lock (_wndLock) {
                if (_wnd != null && !_wnd.IsDisposed) {
                    _wnd.Close();
                    _wnd.Dispose();
                }

                _wnd = new Display2Window();
                _wnd.ParentMetric = this;
            }
        }

        public void OnDraw(Rectangle node, Graphics e) {
            //
        }

        public override void SuspendProcessing() {}
        public override void StartProcessing() {}
        public override void Transfer() {}
    }
}
