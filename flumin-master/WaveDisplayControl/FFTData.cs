using System.Collections.Generic;
using FFTW;

namespace WaveDisplayControl {

    public interface IDataCallback<T> {
        IEnumerable<T> GetData(long index, long count, long step);
        long FillBuffer(long index, long count, long step, T[] buffer);
        long GetLength();
    }

    public class FftData {

        private const int FftSize = 512;

        private readonly FFTWTransform _trans = new FFTWTransform(FftSize);

        public double Samplerate {
            get;
            set;
        }

        private readonly IDataCallback<double> _cb;

        public FftData(IDataCallback<double> cb) {
            _cb = cb;
        }

        public long Length => _cb.GetLength();

        public IEnumerable<double[]> Iterate(long at, long count, long step) {
            var buffer = new double[FftSize];

            for (var i = 0; i < count; i++) {
                var remaining = _cb.FillBuffer(at + i * step, FftSize, 1, buffer);

                if (remaining == FftSize) {
                    yield return new double[0];
                } else {
                    _trans.UseData(buffer, 0, buffer.Length);
                    yield return _trans.Transform();
                }
            }
        }

    }

}
