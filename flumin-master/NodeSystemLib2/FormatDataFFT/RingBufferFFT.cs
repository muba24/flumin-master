using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NodeSystemLib2.FormatDataFFT {
    public class RingBufferFFT {

        private readonly RingBuffer<double> _buffer;

        public int Samplerate { get; }

        public int FFTSize { get; }

        public int FrameSize => FFTSize / 2;

        public int Available => _buffer.Available / FrameSize;

        public int Capacity => _buffer.Capacity / FrameSize;

        public int Free => _buffer.Free / FrameSize;

        public TimeStamp Time { get; private set; }

        private TimeStamp TimeAtReadPosition => Time.Decrement(DistanceReadToWrite * 2, Samplerate);

        private int DistanceReadToWrite {
            get {
                if (_buffer.WritePosition <= _buffer.ReadPosition) {
                    return _buffer.WritePosition + (_buffer.Capacity - _buffer.ReadPosition);
                } else {
                    return _buffer.WritePosition - _buffer.ReadPosition;
                }
            }
        }

        public bool Overflow {
            get { return _buffer.Overflow; }
            set { _buffer.Overflow = value; }
        }

        public RingBufferFFT(int frames, int fftSize, int samplerate) {
            FFTSize = fftSize;
            Samplerate = samplerate;

            _buffer = new RingBuffer<double>(frames * fftSize / 2);
        }

        public int Peek(TimeLocatedBufferFFT target, int frames) {
            var timeBegin = TimeAtReadPosition;
            var read = _buffer.Peek(target.Data, 0, frames * FrameSize);
            if (read % FrameSize != 0) throw new InvalidOperationException("shouldn't dequeue amount which is not a multiple of the frame size");
            target.SetWritten(read / FrameSize, timeBegin.Increment(read * FrameSize, Samplerate));
            return read / FrameSize;
        }

        public int Read(TimeLocatedBufferFFT target, int frames) {
            var timeBegin = TimeAtReadPosition;
            var read = _buffer.Read(target.Data, 0, frames * FrameSize);
            if (read % FrameSize != 0) throw new InvalidOperationException("shouldn't dequeue amount which is not a multiple of the frame size");
            target.SetWritten(read / FrameSize, timeBegin.Increment(read * FrameSize, Samplerate));
            return read / FrameSize;
        }

        public int Write(IReadOnlyTimeLocatedBufferFFT source, int offsetFrames, int frames) {
            var written = _buffer.Write(source.Data, offsetFrames * FrameSize, frames * FrameSize);
            if (written % FrameSize != 0) throw new InvalidOperationException();
            Time = Time.Increment(written * 2, Samplerate);
            return written / FrameSize;
        }

        public int Write(double[] source, int offsetFrames, int frames) {
            var written = _buffer.Write(source, offsetFrames * FrameSize, frames * FrameSize);
            if (written % FrameSize != 0) throw new InvalidOperationException();
            Time = Time.Increment(written * 2, Samplerate);
            return written / FrameSize;
        }

        public int Write(TimeLocatedBufferFFT source, int frames) {
            var written = _buffer.Write(source.Data, 0, frames * FrameSize);
            if (written % FrameSize != 0) throw new InvalidOperationException();
            Time = Time.Increment(written * 2, Samplerate);
            return written / FrameSize;
        }

    }
}
