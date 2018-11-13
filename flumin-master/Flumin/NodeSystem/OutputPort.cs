using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

namespace SimpleADC.NodeSystem {

    
    public class FFTOutputPort : OutputPort {

        private int _samplerate;

        private int _fftSize;


        public FFTOutputPort(string name) : base(name, PortDataType.FFT) {
            //
        }


        public void SendData(TimeLocatedBuffer data) {
            foreach (var input in Connections) {
                ((FFTInputPort)input).RecieveData(data);
            }
        }


        public int FFTSize
        {
            get { return _fftSize; }
            set {
                _fftSize = value;
                foreach (var port in Connections) {
                    if (port is FFTInputPort)
                        ((FFTInputPort)port).FFTSize = _fftSize;
                    else throw new InvalidCastException();
                }
            }
        }


        [ReadOnly(true)]
        public int Samplerate
        {
            get
            {
                return _samplerate;
            }
            set
            {
                if (value != _samplerate) {
                    _samplerate = value;
                    foreach (var port in Connections) {
                        if (port is FFTInputPort)
                            ((FFTInputPort)port).Samplerate = _samplerate;
                        else throw new InvalidCastException();
                    }
                }
            }
        }


        protected override void OnNewConnection(InputPort target) {
            if (target is FFTInputPort)
                ((FFTInputPort)target).Samplerate = _samplerate;
            else throw new InvalidCastException();
        }


    }


    public class ValueOutputPort : OutputPort {

        public ValueOutputPort(string name) : base(name, PortDataType.Value) {
        }

        public void SendData(TimeLocatedValue value) {
            foreach (var input in Connections) {
                ((ValueInputPort)input).RecieveData(value);
            }
        }

    }

    public class DataOutputPort : OutputPort {

        private int _samplerate;


        public DataOutputPort(string name) : base(name, PortDataType.Array) {
        }

        public void SendData(TimeLocatedBuffer data) {
            foreach (var input in Connections) {
                var port = (DataInputPort)input;
                if (port.Queue.Capacity - port.Queue.Length < data.WrittenSamples) {
                    System.Diagnostics.Debug.WriteLine("Waiting while outstanding jobs: " + CustomPool.Forker.CountRunning());
                    while (port.Queue.Capacity - port.Queue.Length < data.WrittenSamples) {
                        System.Threading.Thread.Sleep(0);
                    }
                }
                port.RecieveData(data);
            }
        }

        [ReadOnly(true)]
        public int Samplerate
        {
            get
            {
                return _samplerate;
            }
            set
            {
                if (value != _samplerate) {
                    _samplerate = value;
                    foreach (var port in Connections) {
                        if (port is DataInputPort)
                            ((DataInputPort)port).Samplerate = _samplerate;
                        else if (port is FFTInputPort)
                            ((FFTInputPort)port).Samplerate = _samplerate;
                    }
                }
            }
        }

        protected override void OnNewConnection(InputPort target) {
            if (target is DataInputPort)
                ((DataInputPort)target).Samplerate = _samplerate;
            else throw new InvalidCastException();
        }


    }

    [TypeConverter(typeof(ExpandableObjectConverter))]
    public abstract class OutputPort : IPort {

        private Node _parent;

        private readonly ObservableCollection<InputPort> _connections = new ObservableCollection<InputPort>();

        public event EventHandler OutputConnectionsChanged;

        protected OutputPort(string name, PortDataType type) {
            Name = name;
            DataType = type;
            _connections.CollectionChanged += _connections_CollectionChanged;
        }

        public string Name { get; }

        [Browsable(false)]
        public Node Parent {
            get { return _parent; }
            set {
                if (_parent == null) _parent = value;
                else throw new Exception("Parent can only be set once");
            }
        }


        [Browsable(false)]
        public PortDirection Direction { get; } = PortDirection.Output;

        public PortDataType DataType { get; }

        [Browsable(false)]
        public ObservableCollection<InputPort> Connections => _connections;

        private void _connections_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e) {
            switch (e.Action) {
                case NotifyCollectionChangedAction.Add:
                    foreach (InputPort target in e.NewItems) {
                        OnNewConnection(target);
                    }
                    break;
                case NotifyCollectionChangedAction.Remove:
                    foreach (InputPort target in e.OldItems) {
                        OnRemoveConnection(target);
                    }
                    break;
            }

            OutputConnectionsChanged?.Invoke(this, null);
        }

        protected virtual void OnRemoveConnection(InputPort target) {
            //
        }

        protected virtual void OnNewConnection(InputPort target) {
            //
        }

        public override string ToString() {
            return "Output " + Name;
        }

        public static T Create<T>(string name, Node parent) where T : OutputPort {
            T result;
            if (typeof(T) == typeof(DataOutputPort)) {
                result = (new DataOutputPort(name) as T);
            } else if (typeof(T) == typeof(ValueOutputPort)) {
                result = (new ValueOutputPort(name) as T);
            } else if (typeof(T) == typeof(FFTOutputPort)) {
                result = (new FFTOutputPort(name) as T);
            } else {
                throw new InvalidCastException();
            }
            parent.AddOutput(result);
            return result;
        }

        public static OutputPort Create(string name, PortDataType type) {
            switch (type) {
                case PortDataType.Array: return new DataOutputPort(name);
                case PortDataType.Value: return new ValueOutputPort(name);
                case PortDataType.FFT:   return new FFTOutputPort(name);
                default: throw new ArgumentException();
            }
        }

        public static IEnumerable<OutputPort> CreateMany(params OutputPort[] ports) {
            return ports;
        }
    }

}
