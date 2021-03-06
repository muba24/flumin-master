﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;

namespace SimpleADC.NodeSystem {

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
                    Queue = new SignalRingBuffer(queueSamples < 0 ? GlobalSettings.Instance.RingBufferSize(Samplerate) : queueSamples, Samplerate);
                }

                if (Buffer == null || Buffer.Samplerate != Samplerate || (bufferSamples >= 0 && Buffer.Length < bufferSamples)) {
                    Buffer = TimeLocatedBuffer.Default(Samplerate);
                }

                return true;

            } catch (Exception e) {

                GlobalSettings.Instance.Errors.Add(new Error($"InitBuffer of {Parent.Name}: {e}"));
                return false;

            }
        }

        public void RecieveData(TimeLocatedBuffer buf) {
            if (Queue == null) {
                GlobalSettings.Instance.Errors.Add(new Error("Data Input: Buffer not initialized"));
                GlobalSettings.Instance.StopProcessing();
                return;
            } else if (Queue.Length + buf.WrittenSamples > Queue.Capacity) {
                GlobalSettings.Instance.Errors.Add(new Error("Data Input: Buffer full. Data loss. Stopping"));
                GlobalSettings.Instance.StopProcessing();
            } else {
                Queue.Enqueue(buf);
                DataAvailable?.Invoke(this, Queue.Length);
            }
        }

        public TimeLocatedBuffer Read(int samples = -1) {
            Queue.Dequeue(Buffer, samples);
            return Buffer;
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

        internal void LoadBuffer(SignalRingBuffer buf) {
            Queue.LoadState(buf);
        }
    }

    public class ValueInputPort : InputPort {

        public event EventHandler<TimeLocatedValue> ValueAvailable;

        //[Browsable(false)]
        //public ConcurrentQueue<TimeLocatedValue> Values = new ConcurrentQueue<TimeLocatedValue>();

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
            Queue = new SignalRingBuffer(GlobalSettings.Instance.RingBufferSize(_samplerate), _samplerate);
        }


        public void RecieveData(TimeLocatedBuffer buf) {
            if (buf.Length % (_fftSize / 2) != 0) throw new FFTDataLengthException();

            if (Queue.Length + buf.WrittenSamples >= Queue.Capacity) {
                GlobalSettings.Instance.Errors.Add(new Error("Data Input: Buffer full. Data loss. Stopping"));
                GlobalSettings.Instance.StopProcessing(true);
            } else {
                Queue.Enqueue(buf);
                DataAvailable?.Invoke(this, Queue.Length);
            }
        }


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

        public override string ToString() {
            return "Input " + Name;
        }

        public static T Create<T>(string name, Node parent) where T : InputPort {
            T result;
            if (typeof(T) == typeof(DataInputPort)) {
                result = (new DataInputPort(name) as T);
            } else if (typeof(T) == typeof(ValueInputPort)) {
                result = (new ValueInputPort(name) as T);
            } else if (typeof(T) == typeof(FFTInputPort)) {
                result = (new FFTInputPort(name) as T);
            } else {
                throw new InvalidCastException();
            }
            parent.AddInput(result);
            return result;
        }

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
