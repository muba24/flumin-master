using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Xml;

namespace NodeSystemLib {

    // NOTE: If changing of a running graph will be implemented,
    //       nodes added to it while processing should also be started.
    //       The graph is responsible for this. Now when should this happen?
    //       The graph will notice a new node when it is constructed, as 
    //       the default constructor of a node will add it to the graph
    //       which is subsequently notified. Now this will happen BEFORE
    //       the constructor of the node has finished. There will not even
    //       be ports in the node at the time the graph can start the node.
    //       That means, the graph can't do it. The instantiator of the node
    //       has to do it.


    public class StateNode<T> : Node where T : StateNode<T> {

        // if a comparer is given and the parent node is marked as unique through its
        // Metric attribute, check if the node is actually unique.
        public StateNode(string name, Graph g, Func<T, bool> comparer) : base(name, g) {
            var typeInfo = typeof(T);
            var attrs = typeInfo.GetCustomAttributes(typeof(MetricAttribute), false);
            var attr = (MetricAttribute)attrs?.FirstOrDefault();
            if (attr != null && attr.OnlyOnePerGraph) {
                foreach (var node in g.Nodes.OfType<T>()) {
                    if (node != this && comparer(node)) {
                        Dispose();
                        throw new PortAlreadyExistsException();
                    }
                }
            }
        }

        public StateNode(string name, Graph g) : base(name, g) {
        }

        public override NodeState SaveState() { return NodeState.Save((T)this, Parent.GetCurrentClockTime()); }
        public override void LoadState(NodeState state) { state.Load(); }

    }

    /// <summary>
    /// Basic building block for graphs
    /// </summary>
    public class Node : IDisposable {

        public enum ProcessingState {
            Stopped,
            Suspended,
            Running
        }
        
        private readonly List<OutputPort> _outputs;
        private readonly List<InputPort>  _inputs;

        [TypeConverter(typeof(ExpandableObjectConverter))]
        [Category("Node")]
        public NodePortCollection<InputPort> InputPorts   => new NodePortCollection<InputPort>(_inputs);

        [TypeConverter(typeof(ExpandableObjectConverter))]
        [Category("Node")]
        public NodePortCollection<OutputPort> OutputPorts => new NodePortCollection<OutputPort>(_outputs);

        public class PortChangedEventArgs : EventArgs {
            public IPort Port;
            public int Index;
        }

        public event EventHandler<PortChangedEventArgs> PortAdded;
        public event EventHandler<PortChangedEventArgs> PortRemoved;

        private readonly List<ValueInputPort> _valueInputs = new List<ValueInputPort>();

        private Graph _parent;

        private IMetricFactory _factory;

        private readonly Thread _ownerThread = Thread.CurrentThread;

        [Browsable(false)]
        public ProcessingState State { get; private set; }

        [Browsable(false)]
        public Graph Parent {
            get {
                return _parent;
            }
            set {
                if (_parent != null)
                    _parent.Nodes.Remove(this);
                _parent = value;
                _parent.Nodes.Add(this);
            }
        }

        public string Description { get; set; }

        [Browsable(false)]
        public IMetricFactory Factory {
            get { return _factory; }
            set {
                if (_factory == null) {
                    _factory = value;
                } else {
                    throw new InvalidOperationException("Factory of node can only be set once");
                }
            }
        }

        public void AddInput(InputPort port, int index = -1) {
            if (InputPorts.Any(i => i.Name == port.Name)) {
                throw new ArgumentException("Name of port ambiguous");
            }

            port.InputConnectionChanged += Input_InputConnectionChanged;

            // TODO: cleanup? Cause scheduler does DataAvailable and ValueAvailable now
            switch (port.DataType) {
                case PortDataType.Array:
                    ((DataInputPort)port).DataAvailable += Input_DataAvailable;
                    ((DataInputPort)port).SamplerateChanged += Input_SamplerateChanged;
                    break;
                case PortDataType.FFT:
                    ((FFTInputPort)port).DataAvailable += Input_DataAvailable;
                    ((FFTInputPort)port).SamplerateChanged += Input_SamplerateChanged;
                    ((FFTInputPort)port).FFTSizeChanged += Input_FFTSizeChanged;
                    break;
                case PortDataType.Value:
                    ((ValueInputPort)port).ValueAvailable += Node_ValueAvailable;
                    _valueInputs.Add((ValueInputPort)port);
                    break;
            }

            port.Parent = this;
            if (index > -1) {
                _inputs.Insert(index, port);
            } else {
                _inputs.Add(port);
            }
            PortAdded?.Invoke(this, new PortChangedEventArgs { Port = port, Index = index });
        }

        public void AddOutput(OutputPort port, int index = -1) {
            if (OutputPorts.Any(i => i.Name == port.Name)) {
                throw new ArgumentException("Name of port ambiguous");
            }

            port.OutputConnectionsChanged += Output_OutputConnectionsChanged;
            port.Parent = this;
            if (index > -1) {
                _outputs.Insert(index, port);
            } else {
                _outputs.Add(port);
            }
            PortAdded?.Invoke(this, new PortChangedEventArgs { Port = port, Index = index });
        }

        public void RemoveInput(InputPort port) {
            port.InputConnectionChanged -= Input_InputConnectionChanged;

            switch (port.DataType) {
                case PortDataType.Array:
                    ((DataInputPort)port).DataAvailable -= Input_DataAvailable;
                    ((DataInputPort)port).SamplerateChanged -= Input_SamplerateChanged;
                    break;
                case PortDataType.FFT:
                    ((FFTInputPort)port).DataAvailable -= Input_DataAvailable;
                    ((FFTInputPort)port).SamplerateChanged -= Input_SamplerateChanged;
                    ((FFTInputPort)port).FFTSizeChanged -= Input_FFTSizeChanged;
                    break;
                case PortDataType.Value:
                    ((ValueInputPort)port).ValueAvailable -= Node_ValueAvailable;
                    _valueInputs.Remove((ValueInputPort)port);
                    break;
            }

            var index = _inputs.IndexOf(port);
            _inputs.Remove(port);
            PortRemoved?.Invoke(this, new PortChangedEventArgs { Port = port, Index = index });
        }

        public void RemoveOutput(OutputPort port) {
            port.OutputConnectionsChanged -= Output_OutputConnectionsChanged;
            var index = _outputs.IndexOf(port);
            _outputs.Remove(port);
            PortRemoved?.Invoke(this, new PortChangedEventArgs { Port = port, Index = index });
        }

        protected Node(string name, Graph g) {
            Name        = name;
            Parent      = g;
            Description = Name;

            _outputs = new List<OutputPort>();
            _inputs  = new List<InputPort>();
        }

        protected Node(string name, Graph graph, IEnumerable<InputPort> inputs, IEnumerable<OutputPort> outputs) : this(name, graph) {
            foreach (var port in inputs) {
                AddInput(port);
            }

            foreach (var port in outputs) {
                AddOutput(port);
            }
        }


        private readonly object _fftChangedLock = new object();
        private void Input_FFTSizeChanged(object sender, int e) {
            lock (_fftChangedLock) {
                FFTSizeChanged(sender as FFTInputPort);
            }
        }

        public void Serialize(XmlWriter writer) {
            writer.WriteStartElement("node");
            Serializing(writer);
            writer.WriteEndElement();
        }

        private void Node_ValueAvailable(object sender, TimeLocatedValue e) {
            //CustomPool.Forker.Fork(ValueProcessor);
        }

        public virtual bool PrepareProcessing() {
            foreach (var input in InputPorts.OfType<DataInputPort>()) {
                input.InitBuffer();
            }

            foreach (var input in InputPorts.OfType<FFTInputPort>()) {
                input.InitBuffer();
            }

            foreach (var input in InputPorts.OfType<ValueInputPort>()) {
                //
            }

            return true;
        }

        public void StartProcessing() {
            if (State == ProcessingState.Stopped) {
                State = ProcessingState.Running;
                ProcessingStarted();
            } else {
                throw new InvalidOperationException("Cannot start processing. Current state != stopped");
            }
        }

        public void SuspendProcessing() {
            lock(_dataLock) {
                lock (_valueLock) {
                    if (State != ProcessingState.Running) {
                        throw new InvalidOperationException("Cannot suspend processing. Current state != running");
                    }
                    State = ProcessingState.Suspended;
                    ProcessingSuspended();
                }
            }
        }

        public void StopProcessing() {
            if (State != ProcessingState.Stopped) {
                State = ProcessingState.Stopped;
                ProcessingStopped();
            } else {
                throw new InvalidOperationException("Cannot stop processing. Already stopped.");
            }
        }

        private readonly object _valueLock = new object();
        private void ValueProcessor() {
            lock (_valueLock) {
                if (State != ProcessingState.Running) return;

                foreach (var port in _valueInputs) {
                    ValueAvailable(port);
                }
            }
        }

        private readonly object _dataLock = new object();
        private void DataProcessor(DataInputPort port) {
            lock (_dataLock) {
                if (State != ProcessingState.Running) return;

                //var queuelen = port.Queue?.Length ?? 0;
                //var buflen   = port.Buffer?.Length ?? 1;
                //var packets  = queuelen / buflen;

                //for (int i = 0; i < packets; i++) {
                    DataAvailable(port);
                //}

                //if (State == ProcessingState.Running && port.Queue != null) {
                //    if (port.Queue.Length > 0) {
                //        CustomPool.Forker.Fork(() => DataProcessor((DataInputPort)port));
                //    }
                //}
            }
        }

        private void DataFftProcessor(FFTInputPort port) {
            lock (_dataLock) {
                if (State != ProcessingState.Running) return;

                // Graph wont start if FFTSize is 0, don't have to check for DivByZero
                //var queuelen = port.Queue?.Length ?? 0;
                //var buflen   = port.FFTSize;
                //var packets  = queuelen / buflen;

                //for (int i = 0; i < packets; i++) {
                    FftDataAvailable(port);
                //}
            }
        }

        public void TriggerProcessing(InputPort p) {
            switch (p.DataType) {
                case PortDataType.Array:
                    DataProcessor((DataInputPort)p);
                    break;
                case PortDataType.FFT:
                    DataFftProcessor((FFTInputPort)p);
                    break;
                case PortDataType.Value:
                    ValueAvailable((ValueInputPort)p);
                    break;
            }
        }

        private void Input_DataAvailable(object sender, int e) {
            //if (sender is DataInputPort) {
            //    CustomPool.Forker.Fork(() => DataProcessor((DataInputPort)sender));
            //} else if (sender is FFTInputPort) {
            //    CustomPool.Forker.Fork(() => DataFftProcessor((FFTInputPort)sender));
            //}
        }

        private readonly object _outConChangedLocker = new object();
        private void Output_OutputConnectionsChanged(object sender, EventArgs e) {
            lock (_outConChangedLocker) {
                OutputConnectionsChanged();
            }
        }

        private readonly object _sampChangedLocker = new object();
        private void Input_SamplerateChanged(object sender, int e) {
            lock (_sampChangedLocker) {
                System.Diagnostics.Debug.Assert(State == ProcessingState.Stopped);
                InputSamplerateChanged(sender as InputPort);
            }
        }

        private readonly object _inpConChangedLocker = new object();
        private void Input_InputConnectionChanged(object sender, OutputPort e) {
            lock (_inpConChangedLocker) {
                InputConnectionChanged(sender as InputPort, e);
            }
        }

        protected virtual void Serializing(XmlWriter writer) {
            writer.WriteAttributeString("name", Name);
            writer.WriteAttributeString("type", GetType().FullName);
            writer.WriteAttributeString("description", Description);

            writer.WriteStartElement("InputPorts");
            foreach (var input in InputPorts) {
                SerializePort(input, writer);
            }
            writer.WriteEndElement();

            writer.WriteStartElement("OutputPorts");
            foreach (var output in OutputPorts) {
                SerializePort(output, writer);
            }
            writer.WriteEndElement();
        }

        private void SerializePort(OutputPort p, XmlWriter w) {
            w.WriteStartElement("OutputPort");
            w.WriteAttributeString("name", p.Name);
            w.WriteAttributeString("type", p.DataType.ToString());
            w.WriteEndElement();
        }

        private void SerializePort(InputPort p, XmlWriter w) {
            w.WriteStartElement("InputPort");
            w.WriteAttributeString("name", p.Name);
            w.WriteAttributeString("type", p.DataType.ToString());
            w.WriteEndElement();
        }


        protected virtual void ValueAvailable(ValueInputPort port) { }
        protected virtual void DataAvailable(DataInputPort port) { }
        protected virtual void FftDataAvailable(FFTInputPort port) { }
        protected virtual void OutputConnectionsChanged() { }
        protected virtual void InputSamplerateChanged(InputPort e) { }
        protected virtual void FFTSizeChanged(InputPort e) { }
        protected virtual void InputConnectionChanged(InputPort input, OutputPort newTarget) { }
        protected virtual void ProcessingStarted() { }
        protected virtual void ProcessingStopped() { }
        protected virtual void ProcessingSuspended() { }
        public virtual FlushState FlushData() { return FlushState.Empty; }
        public virtual NodeState SaveState() { return null; }
        public virtual void LoadState(NodeState state) { }

        public enum FlushState {
            Empty,
            Some
        }

        [Browsable(false)]
        public bool InputsValid { get; protected set; }

        [Category("Node")]
        public string Name { get; }



        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing) {
            if (!disposedValue) {
                _parent.Nodes.Remove(this);
                disposedValue = true;
            }
        }

        public void Dispose() {
            Dispose(true);
        }
        #endregion

    }

    /// <summary>
    /// Collection for display of ports in Property Editor
    /// </summary>
    /// <typeparam name="T">Must inherit from IPort</typeparam>
    public class NodePortCollection<T> : IEnumerable<T>, ICustomTypeDescriptor where T : IPort {
        private readonly IList<T> _elems;

        public NodePortCollection(IList<T> elems) {
            _elems = elems;
        }

        public int Count => _elems.Count;

        public T this[int index] => _elems[index];

        public override string ToString() => "Collection";

        public AttributeCollection GetAttributes() => TypeDescriptor.GetAttributes(this, true);

        public string GetClassName() => TypeDescriptor.GetClassName(this, true);

        public string GetComponentName() => TypeDescriptor.GetComponentName(this, true);

        public TypeConverter GetConverter() => TypeDescriptor.GetConverter(this, true);

        public EventDescriptor GetDefaultEvent() => TypeDescriptor.GetDefaultEvent(this, true);

        public PropertyDescriptor GetDefaultProperty() => TypeDescriptor.GetDefaultProperty(this, true);

        public object GetEditor(Type editorBaseType) => TypeDescriptor.GetEditor(this, editorBaseType, true);

        public EventDescriptorCollection GetEvents() => TypeDescriptor.GetEvents(this, true);

        public EventDescriptorCollection GetEvents(Attribute[] attributes) => TypeDescriptor.GetEvents(this, attributes, true);

        public PropertyDescriptorCollection GetProperties() {
            var pds = new PropertyDescriptorCollection(null);

            for (var i = 0; i < _elems.Count; i++) {
                pds.Add(new PortCollectionPropertyDescriptor<T>(this, i));
            }

            return pds;
        }

        public PropertyDescriptorCollection GetProperties(Attribute[] attributes) => GetProperties();

        public object GetPropertyOwner(PropertyDescriptor pd) => this;

        public IEnumerator<T> GetEnumerator() => _elems.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    /// <summary>
    /// Description of ports for Property Editor
    /// </summary>
    /// <typeparam name="T">Must inherit from IPort</typeparam>
    public class PortCollectionPropertyDescriptor<T> : PropertyDescriptor where T : IPort {

        private readonly NodePortCollection<T> _collection;
        private readonly int _index;

        public PortCollectionPropertyDescriptor(NodePortCollection<T> coll, int idx) : base("#"+idx, null) {
            _collection = coll;
            _index = idx;
        }

        public override AttributeCollection Attributes => new AttributeCollection(null);

        public override bool CanResetValue(object component) => true;

        public override Type ComponentType => _collection.GetType();

        public override string DisplayName => _collection[_index].ToString();

        public override string Description => _collection[_index].ToString();

        public override object GetValue(object component) => _collection[_index];

        public override bool IsReadOnly => true;

        public override string Name => "#" + _index;

        public override Type PropertyType => _collection[_index].GetType();

        public override void ResetValue(object component) { }

        public override bool ShouldSerializeValue(object component) => true;

        public override void SetValue(object component, object value) { }

    }
}
