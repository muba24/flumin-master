using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Xml;

namespace SimpleADC.NodeSystem {

    public class StateNode<T> : Node where T : StateNode<T> {

        public StateNode(string name, Graph g) : base(name, g) { }

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

        public event EventHandler<IPort> PortAdded;
        public event EventHandler<IPort> PortRemoved;

        private readonly List<ValueInputPort> _valueInputs = new List<ValueInputPort>();

        private Graph _parent;

        private readonly Thread _ownerThread = Thread.CurrentThread;

        public ProcessingState State { get; private set; }

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

        public void AddInput(InputPort port) {
            port.InputConnectionChanged += Input_InputConnectionChanged;

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
            _inputs.Add(port);
            PortAdded?.Invoke(this, port);
        }

        public void AddOutput(OutputPort port) {
            port.OutputConnectionsChanged += Output_OutputConnectionsChanged;
            port.Parent = this;
            _outputs.Add(port);
            PortAdded?.Invoke(this, port);
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

            _inputs.Remove(port);
            PortRemoved?.Invoke(this, port);
        }

        public void RemoveOutput(OutputPort port) {
            port.OutputConnectionsChanged -= Output_OutputConnectionsChanged;
            _outputs.Remove(port);
            PortRemoved?.Invoke(this, port);
        }

        protected Node(string name, Graph g) {
            Name   = name;
            Parent = g;

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
            writer.WriteAttributeString("type", GetType().FullName);
            Serializing(writer);
            writer.WriteEndElement();
        }

        private void Node_ValueAvailable(object sender, TimeLocatedValue e) {
            CustomPool.Forker.Fork(ValueProcessor);
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

                var queuelen = port.Queue?.Length ?? 0;
                var buflen   = port.Buffer?.Length ?? 1;
                var packets  = queuelen / buflen;

                for (int i = 0; i < packets; i++) {
                    DataAvailable(port);
                }
            }
        }

        private void Input_DataAvailable(object sender, int e) {
            CustomPool.Forker.Fork(() => DataProcessor((DataInputPort)sender));
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

        protected virtual void ValueAvailable(ValueInputPort port) { }
        protected virtual void DataAvailable(DataInputPort port) { }
        protected virtual void OutputConnectionsChanged() { }
        protected virtual void InputSamplerateChanged(InputPort e) { }
        protected virtual void FFTSizeChanged(InputPort e) { }
        protected virtual void InputConnectionChanged(InputPort input, OutputPort newTarget) { }
        protected virtual void ProcessingStarted() { }
        protected virtual void ProcessingStopped() { }
        protected virtual void ProcessingSuspended() { }
        protected virtual void Serializing(XmlWriter writer) { }
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
