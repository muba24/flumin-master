using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NodeSystemLib2.FormatDataFFT {

    public class InputPortDataFFT : InputPort {

        private int _samplerate;

        private int _fftSize;

        private TimeLocatedBufferFFT _readBuffer;

        private RingBufferFFT _queue;

        private TimeStamp _stamp;

        public event EventHandler<SamplerateChangedEventArgs> SamplerateChanged;

        public InputPortDataFFT(Node parent, string name) : base(parent, name, PortDataTypes.TypeIdFFT) {
            FFTSize = 512;
        }

        public int Samplerate {
            get { return _samplerate; }
            set {
                if (value < 0) throw new InvalidOperationException();
                _samplerate = value;
                SamplerateChanged?.Invoke(this, new SamplerateChangedEventArgs(value));
            }
        }

        public int FFTSize {
            get {
                return _fftSize;
            }

            set {
                if (value < 1) throw new InvalidOperationException();
                if (!IsPowerOfTwo(value)) throw new ArgumentException();
                _fftSize = value;
            }
        }

        public int FrameSize => FFTSize / 2;

        // http://stackoverflow.com/a/600306
        private bool IsPowerOfTwo(int x) {
            return (x != 0) && ((x & (x - 1)) == 0);
        }

        public void PrepareProcessing(int queueFFTs, int bufferFFTs) {
            _queue      = new RingBufferFFT(queueFFTs, FFTSize, Samplerate);
            _readBuffer = new TimeLocatedBufferFFT(bufferFFTs, FFTSize, Samplerate);
            _stamp      = TimeStamp.Zero;
        }

        public TimeStamp Time => _stamp;

        /// <summary>
        /// Number of FFTs available in the queue
        /// </summary>
        public int Available => _queue?.Available ?? 0;

        /// <summary>
        /// Number of FFTs that can still fit into the queue
        /// </summary>
        public int Free => _queue?.Free ?? 0;

        /// <summary>
        /// Number of FFTs in total that can fit in the queue
        /// </summary>
        public int Capacity => _queue?.Capacity ?? 0;

        /// <summary>
        /// Number of FFTs the read buffer can hold
        /// </summary>
        public int BufferCapacity => _readBuffer?.Capacity ?? 0;


        public int Write(IReadOnlyTimeLocatedBufferFFT buffer) {
            return Write(buffer, 0, buffer.Available);
        }

        public int Write(IReadOnlyTimeLocatedBufferFFT buffer, int offset, int frameCount) {
            if (buffer.Samplerate != Samplerate) throw new InvalidOperationException();
            if (buffer.FFTSize != FFTSize) throw new InvalidOperationException();

            var written = _queue.Write(buffer, offset, frameCount);
            _stamp = _stamp.Increment(written * FFTSize, Samplerate);
            return written;
        }

        public IReadOnlyTimeLocatedBufferFFT Read() {
            return Read(_readBuffer.Capacity);
        }

        public IReadOnlyTimeLocatedBufferFFT Read(int frames) {
            frames = Math.Min(frames, Available);
            _queue.Read(_readBuffer, frames);
            return _readBuffer;
        }
    }
}
