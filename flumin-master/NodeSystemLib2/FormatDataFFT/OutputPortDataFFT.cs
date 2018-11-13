using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NodeSystemLib2.FormatDataFFT {
    public class OutputPortDataFFT : OutputPort {

        private RingBufferFFT _queue;

        private TimeLocatedBufferFFT _transferBuffer;

        private int _samplerate;
        private int _fftSize;

        public OutputPortDataFFT(Node parent, string name) 
            : base(parent, name, PortDataTypes.TypeIdFFT) {
            ConnectionAdded += OutputPortDataFFT_ConnectionAdded;
        }

        private void OutputPortDataFFT_ConnectionAdded(object sender, ConnectionModifiedEventArgs e) {
            if (e.Action == ConnectionModifiedEventArgs.Modifier.Added) {
                ((InputPortDataFFT)e.Connection).FFTSize = FFTSize;
            }
        }

        public event EventHandler<SamplerateChangedEventArgs> SamplerateChanged;

        public int FramesAvailable => _queue?.Available ?? 0;

        public int Capacity => _queue?.Capacity ?? 0;

        public int FFTSize {
            get { return _fftSize; }
            set {
                if (value < 0) throw new ArgumentOutOfRangeException();
                if (!IsPowerOfTwo(value)) throw new ArgumentException();
                _fftSize = value;

                foreach (var connection in Connections) {
                    ((InputPortDataFFT)connection).FFTSize = _fftSize;
                }

                SamplerateChanged?.Invoke(this, new SamplerateChangedEventArgs(value));
            }
        }

        public int FrameSize => FFTSize / 2;

        // http://stackoverflow.com/a/600306
        private bool IsPowerOfTwo(int x) {
            return (x != 0) && ((x & (x - 1)) == 0);
        }

        public int Samplerate {
            get { return _samplerate; }
            set {
                if (value < 0) throw new ArgumentOutOfRangeException();
                _samplerate = value;

                foreach (var connection in Connections) {
                    ((InputPortDataFFT)connection).Samplerate = _samplerate;
                }

                SamplerateChanged?.Invoke(this, new SamplerateChangedEventArgs(value));
            }
        }

        public int Free => (FrameSize == 0) ? 0 : _queue.Free;

        public override void AddConnection(InputPort port) {
            base.AddConnection(port);
            ((InputPortDataFFT)port).Samplerate = Samplerate;
        }

        public void WriteFrame(double[] data, int offset) {
            var written = _queue.Write(data, offset, 1);
            if (written != 1) throw new ArithmeticException();
        }

        public void PrepareProcessing(int queueSizeFrames, int transferBufferSizeFrames) {
            if (Samplerate <= 0) throw new ArgumentOutOfRangeException(nameof(Samplerate));
            if (queueSizeFrames < 0) throw new ArgumentOutOfRangeException(nameof(queueSizeFrames), "must be >= 0");
            if (transferBufferSizeFrames < 0) throw new ArgumentOutOfRangeException(nameof(transferBufferSizeFrames), "must be >= 0");

            _queue = new RingBufferFFT(queueSizeFrames, FFTSize, Samplerate);
            _transferBuffer = new TimeLocatedBufferFFT(transferBufferSizeFrames, FFTSize, Samplerate);
        }

        public void Transfer() {
            var minFree = Connections.Count > 0 ? Connections.Min(c => ((InputPortDataFFT)c).Free) : int.MaxValue;
            var count = Math.Min(_transferBuffer.Capacity, minFree);

            var read = _queue.Read(_transferBuffer, count);

            foreach (var input in Connections) {
                var written = ((InputPortDataFFT)input).Write(_transferBuffer);
                if (written != read) {
                    throw new InvalidOperationException();
                }
            }
        }

    }
}
