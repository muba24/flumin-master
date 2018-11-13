using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NodeSystemLib2.FormatData1D {
    public class RingBuffer1D<T> where T : struct {

        private readonly RingBuffer<T> _buffer;

        public RingBuffer1D(int elements, int samplerate) {
            _buffer = new RingBuffer<T>(elements);
            Samplerate = samplerate;
        }

        public bool Overflow {
            get { return _buffer.Overflow; }
            set { _buffer.Overflow = value; }
        }

        public int Available => _buffer.Available;

        public int Capacity => _buffer.Capacity;

        public int Free => _buffer.Free;

        public int Samplerate { get; }

        public TimeStamp Time { get; private set; }

        private TimeStamp TimeAtReadPosition => Time.Decrement(DistanceReadToWrite, Samplerate);

        private int DistanceReadToWrite {
            get {
                if (_buffer.WritePosition <= _buffer.ReadPosition) {
                    return _buffer.WritePosition + (_buffer.Capacity - _buffer.ReadPosition);
                } else {
                    return _buffer.WritePosition - _buffer.ReadPosition;
                }
            }
        }

        public int Peek(TimeLocatedBuffer1D<T> target, int elements) {
            var timeBegin = TimeAtReadPosition;
            var read = _buffer.Peek(target.Data, 0, elements);
            target.SetWritten(read, timeBegin.Increment(read, Samplerate));
            return read;
        }

        public int Read(TimeLocatedBuffer1D<T> target) {
            return Read(target, target.Capacity);
        }

        public int Read(TimeLocatedBuffer1D<T> target, int elements) {
            var timeBegin = TimeAtReadPosition;
            var read = _buffer.Read(target.Data, 0, elements);
            target.SetWritten(read, timeBegin.Increment(read, Samplerate));
            return read;
        }

        public int Write(IReadOnlyTimeLocatedBuffer1D<T> source, int offset, int elements) {
            var written = _buffer.Write(source.Data, offset, elements);
            Time = Time.Increment(written, Samplerate);
            return written;
        }

        public int Write(T[] source, int offset, int elements) {
            var written = _buffer.Write(source, offset, elements);
            Time = Time.Increment(written, Samplerate);
            return written;
        }

        public int Write(T[,] source, int offset, int elements) {
            var written = _buffer.Write(source, offset, elements);
            Time = Time.Increment(written, Samplerate);
            return written;
        }

        public int Write(IntPtr source, int offset, int elements) {
            var written = _buffer.Write(source, offset, elements);
            Time = Time.Increment(written, Samplerate);
            return written;
        }

        public int Write(TimeLocatedBuffer1D<T> source) {
            return Write(source, source.Available);
        }

        public int Write(TimeLocatedBuffer1D<T> source, int elements) {
            var written = _buffer.Write(source.Data, 0, elements);
            Time = Time.Increment(written, Samplerate);
            return written;
        }

    }
}
