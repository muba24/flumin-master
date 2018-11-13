using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NodeSystemLib2.FormatDataFFT {

    public class TimeLocatedBufferFFT : TimeLocatedBuffer, IReadOnlyTimeLocatedBufferFFT {

        private readonly double[] _buffer;

        private readonly int _fftSize;

        private readonly int _samplerate;

        public int Capacity => _buffer.Length / FrameSize;

        public int Available { get; private set; }

        public int FFTSize => _fftSize;

        public int FrameSize => _fftSize / 2;

        public int Samplerate => _samplerate;

        public TimeLocatedBufferFFT(int fftCount, int fftSize, int samplerate) {
            _buffer = new double[fftCount * fftSize / 2];
            _fftSize = fftSize;
            _samplerate = samplerate;
        }

        public double[] Data => _buffer;

        public void SetWritten(int frameCount) {
            Available = frameCount;
            Time = Time.Increment(frameCount * _fftSize, _samplerate);
        }

        public void SetWritten(int frameCount, TimeStamp time) {
            Available = frameCount;
            Time = time;
        }

        public void SetFrameData(double[] src, int offset, int frameCount) {
            if (frameCount > Capacity) throw new InsufficientMemoryException();

            Array.Copy(src, offset, _buffer, 0, frameCount * FrameSize);
            SetWritten(frameCount);
        }

        public void ReadFrameData(double[] dst, int offset, int frameCount) {
            if (frameCount > Available) throw new ArgumentOutOfRangeException();

            Array.Copy(_buffer, 0, dst, offset, frameCount * FrameSize);
        }

        public IEnumerator<IReadOnlyList<double>> GetEnumerator() {
            for (int i = 0; i < Available; i++) {
                yield return new ArraySegment<double>(_buffer, i * FrameSize, FrameSize);
            }
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return GetEnumerator();
        }
    }

    public interface IReadOnlyTimeLocatedBufferFFT : IEnumerable<IReadOnlyList<double>> {

        int Samplerate { get; }

        int FFTSize { get; }

        int FrameSize { get; }

        int Capacity { get; }

        int Available { get; }

        double[] Data { get; }

        TimeStamp Time { get; }

        void ReadFrameData(double[] dst, int offset, int frameCount);

    }

}
