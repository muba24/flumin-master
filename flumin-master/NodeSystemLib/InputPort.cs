using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;

namespace NodeSystemLib {

    public class DataInputPort : InputPort {

        public event EventHandler<int> DataAvailable;

        [Browsable(false)]
        public SignalRingBuffer Queue { get; private set; }

        [Browsable(false)]
        public TimeLocatedBuffer Buffer { get; private set; }

        private int _samplerate;

        public event EventHandler<int> SamplerateChanged;

        public DataInputPort(string name) : base(name, PortDataType.Array) {
        }

        public bool InitBuffer(int queueSamples = -1, int bufferSamples = -1) {
            try {

                if (Queue == null || Queue.Samplerate != Samplerate || (queueSamples >= 0 && Queue.Capacity < queueSamples)) {
                    Queue = new SignalRingBuffer(queueSamples < 0 ? NodeSystemSettings.Instance.SystemHost.GetDefaultRingBufferSize(Samplerate) : queueSamples, Samplerate);
                }

                if (Buffer == null || Buffer.Samplerate != Samplerate || (bufferSamples >= 0 && Buffer.Length < bufferSamples)) {
                    Buffer = TimeLocatedBuffer.Default(Samplerate);
                }

                Queue.Clear();
                Buffer.SetTime(TimeStamp.Zero());

                return true;

            } catch (Exception e) {

                NodeSystemSettings.Instance.SystemHost.ReportError(Parent, $"InitBuffer of {Parent.Name}: {e}");
                return false;

            }
        }

        public int RecieveData(TimeLocatedBuffer buf, int offset, int sampleCount) {
            if (Queue == null) {
                NodeSystemSettings.Instance.SystemHost.ReportError(Parent, "Data Input: Buffer not initialized");
                NodeSystemSettings.Instance.SystemHost.StopProcessing();
                return -1;
            } else if (Queue.Length == Queue.Capacity) {
                NodeSystemSettings.Instance.SystemHost.ReportError(Parent, "Data Input: Buffer full. Data loss. Stopping");
                NodeSystemSettings.Instance.SystemHost.StopProcessing();
                return -1;
            } else {
                var written = Queue.Enqueue(buf, offset, sampleCount);
                DataAvailable?.Invoke(this, Queue.Length);
                return written;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="buf"></param>
        /// <returns>Returns number of samples taken from buffer</returns>
        public int RecieveData(TimeLocatedBuffer buf) {
            return RecieveData(buf, 0, buf.WrittenSamples);
        }

        public TimeLocatedBuffer Read(int samples = -1) {
            Queue.Dequeue(Buffer, samples);
            return Buffer;
        }

        [Browsable(false)]
        public bool CanFillWholeBuffer => Queue.Length >= Buffer.Length;

        [ReadOnly(true)]
        public int Samplerate
        {
            get { return _samplerate; }
            set
            {
                if (value != _samplerate) {
                    _samplerate = value;
                    SamplerateChanged?.Invoke(this, _samplerate);
                }
            }
        }

        internal void LoadBuffer(SignalRingBuffer buf) {
            Queue.LoadState(buf);
        }
    }


    public class EventInputPort : InputPort {

        public class EventEventArgs : EventArgs {
            public TimeStamp Stamp { get; }
            public EventEventArgs(TimeStamp stamp) {
                Stamp = stamp;
            }
        }

        public event EventHandler<EventEventArgs> EventRaised;

        public EventInputPort(string name) : base(name, PortDataType.Event) {
        }

        public void Trigger(TimeStamp stamp) {
            EventRaised?.Invoke(this, new EventEventArgs(stamp));
        }

    }


    public class ValueInputPort : InputPort {

        public event EventHandler<TimeLocatedValue> ValueAvailable;

        [Browsable(false)]
        public ConcurrentTreeBag<TimeLocatedValue> Values = new ConcurrentTreeBag<TimeLocatedValue>(new TimeLocatedValueComparer());

        public ValueInputPort(string name) : base(name, PortDataType.Value) {
        }

        public void RecieveData(TimeLocatedValue value) {
            Values.SafeEnqueue(value);
            ValueAvailable?.Invoke(this, null); 
        }

    }


    public class FFTInputPort : InputPort {

        public class FFTDataLengthException : Exception { }

        private int _samplerate;

        private int _fftSize;

        public event EventHandler<int> SamplerateChanged;

        public event EventHandler<int> FFTSizeChanged;

        public event EventHandler<int> DataAvailable;

        [Browsable(false)]
        public SignalRingBuffer Queue { get; private set; }


        public FFTInputPort(string name) : base(name, PortDataType.FFT) {
            //
        }


        public void InitBuffer() {
            Queue = new SignalRingBuffer(NodeSystemSettings.Instance.SystemHost.GetDefaultRingBufferSize(_samplerate), _samplerate);
        }


        public bool InitBuffer(int queueSamples = -1) {
            try {

                if (Queue == null || Queue.Samplerate != Samplerate || (queueSamples >= 0 && Queue.Capacity < queueSamples)) {
                    Queue = new SignalRingBuffer(queueSamples < 0 ? NodeSystemSettings.Instance.SystemHost.GetDefaultRingBufferSize(Samplerate) : queueSamples, Samplerate);
                }

                //if (Buffer == null || Buffer.Samplerate != Samplerate || (bufferSamples >= 0 && Buffer.Length < bufferSamples)) {
                //    Buffer = TimeLocatedBuffer.Default(Samplerate);
                //}

                Queue.Clear();

                return true;

            } catch (Exception e) {

                NodeSystemSettings.Instance.SystemHost.ReportError(Parent, $"InitBuffer of {Parent.Name}: {e}");
                return false;

            }
        }

        public int Read(TimeLocatedBufferFFT buf) {
            var count = Math.Min(buf.FrameCapacity, Queue.Length / FrameSize);
            return Queue.Dequeue(buf, count * FrameSize);
        }

        public int RecieveData(TimeLocatedBuffer buf, int offset, int sampleCount) {
            if (buf.Length % (_fftSize / 2) != 0) throw new FFTDataLengthException();

            if (Queue.Length == Queue.Capacity) {
                NodeSystemSettings.Instance.SystemHost.ReportError(Parent, "Data Input: Buffer full. Data loss. Stopping");
                NodeSystemSettings.Instance.SystemHost.StopProcessing();
                return -1;
            } else {
                var written = Queue.Enqueue(buf, offset, sampleCount);
                DataAvailable?.Invoke(this, Queue.Length);
                return written;
            }
        }

        public int RecieveData(TimeLocatedBuffer buf) {
            return RecieveData(buf, 0, buf.WrittenSamples);
        }

        internal void LoadBuffer(SignalRingBuffer buf) {
            Queue.LoadState(buf);
        }

        public int FrameSize => FFTSize / 2;

        [ReadOnly(true)]
        public int FFTSize
        {
            get { return _fftSize; }
            set
            {
                if (value != _fftSize) {
                    _fftSize = value;
                    FFTSizeChanged?.Invoke(this, _fftSize);
                }
            }
        }


        [ReadOnly(true)]
        public int Samplerate
        {
            get { return _samplerate; }
            set
            {
                if (value != _samplerate) {
                    _samplerate = value;
                    SamplerateChanged?.Invoke(this, _samplerate);
                }
            }
        }

    }


    [TypeConverter(typeof(ExpandableObjectConverter))]
    public abstract class InputPort : IPort {

        private OutputPort _connection;
        private Node _parent;

        public event EventHandler<OutputPort> InputConnectionChanged;

        protected InputPort(string name, PortDataType type) {
            Name = name;
            DataType = type;
        }

        public string Name { get; }

        [Browsable(false)]
        public Node Parent {
            get {
                return _parent;
            }
            set {
                if (_parent == null) _parent = value;
                else throw new Exception("Parent can only be set once");
            }
        }

        [Browsable(false)]
        public OutputPort Connection {
            get {
                return _connection;
            }
            set {
                _connection = value;
                InputConnectionChanged?.Invoke(this, _connection);
            }
        }

        [Browsable(false)]
        public PortDirection Direction { get; } = PortDirection.Input;

        public PortDataType DataType { get; }

        [Browsable(false)]
        public Graph Graph => Parent.Parent;

        public override string ToString() {
            return "Input " + Name;
        }

        public static T Create<T>(string name, Node parent, int index = -1) where T : InputPort {
            T result;

            if (typeof(T) == typeof(DataInputPort)) {
                result = (new DataInputPort(name) as T);
            } else if (typeof(T) == typeof(ValueInputPort)) {
                result = (new ValueInputPort(name) as T);
            } else if (typeof(T) == typeof(FFTInputPort)) {
                result = (new FFTInputPort(name) as T);
            } else if (typeof(T) == typeof(EventInputPort)) {
                result = (new EventInputPort(name) as T);
            } else {
                throw new InvalidCastException();
            }

            if (result == null) throw new InvalidCastException();
            parent.AddInput(result, index);
            return result;
        }

        [Obsolete]
        public static InputPort Create(string name, PortDataType type) {
            switch (type) {
                case PortDataType.Array: return new DataInputPort(name);
                case PortDataType.Value: return new ValueInputPort(name);
                case PortDataType.FFT:   return new FFTInputPort(name);
                default: throw new ArgumentException();
            }
        }

        public static IEnumerable<InputPort> CreateMany(params InputPort[] ports) {
            return ports;
        }
    }

}
