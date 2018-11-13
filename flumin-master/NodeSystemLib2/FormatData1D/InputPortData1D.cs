using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NodeSystemLib2.FormatData1D {

    public class InputPortData1D : InputPort {

        private TimeLocatedBuffer1D<double> _readBuffer;

        private RingBuffer1D<double> _queue;

        private int _samplerate;

        public event EventHandler<SamplerateChangedEventArgs> SamplerateChanged;

        public InputPortData1D(Node parent, string name) : base(parent, name, PortDataTypes.TypeIdSignal1D) { }

        public int Samplerate {
            get { return _samplerate; }
            set {
                if (value < 0) throw new ArgumentOutOfRangeException();

                _samplerate = value;
                SamplerateChanged?.Invoke(this, new SamplerateChangedEventArgs(value));
            }
        }

        public void PrepareProcessing() {
            PrepareProcessing(
                DefaultParameters.DefaultQueueMilliseconds.ToSamples(Samplerate), 
                DefaultParameters.DefaultBufferMilliseconds.ToSamples(Samplerate)
            );
        }

        public void PrepareProcessing(int queueSize, int bufferSize) {
            if (queueSize < 0) throw new ArgumentOutOfRangeException();
            if (bufferSize < 0) throw new ArgumentOutOfRangeException();

            _queue      = new RingBuffer1D<double>(queueSize, Samplerate);
            _readBuffer = new TimeLocatedBuffer1D<double>(bufferSize, Samplerate);
        }

        public int Write(IReadOnlyTimeLocatedBuffer1D<double> buffer) {
            if (buffer == null) throw new ArgumentNullException();
            return Write(buffer, 0, buffer.Available);
        }

        public int Write(IReadOnlyTimeLocatedBuffer1D<double> buffer, int offset, int count) {
            if (buffer == null) throw new ArgumentNullException();
            if (count < 0 || offset < 0) throw new ArgumentOutOfRangeException();
            return _queue.Write(buffer, offset, count);
        }

        public TimeStamp Time => _queue?.Time ?? TimeStamp.Zero;

        public int Available => _queue?.Available ?? 0;

        public int Free => _queue?.Free ?? 0;

        public int Capacity => _queue?.Capacity ?? 0;

        public int BufferCapacity => _readBuffer?.Capacity ?? 0;

        public IReadOnlyTimeLocatedBuffer1D<double> Read() {
            return Read(_readBuffer.Capacity);
        }

        public IReadOnlyTimeLocatedBuffer1D<double> Read(int elements) {
            if (elements < 0) throw new ArgumentOutOfRangeException();
            if (elements > _readBuffer.Capacity) elements = _readBuffer.Capacity;
            _queue.Read(_readBuffer, elements);
            return _readBuffer;
        }

    }
}
