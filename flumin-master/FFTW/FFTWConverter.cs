using System;
using System.Linq;
using System.Runtime.InteropServices;
using Yeppp;
using Math = System.Math;

namespace FFTW {
    public class FFTWTransform : IDisposable {

        public int Size { get; }

        public IntPtr DataInPtr  { get; }
        public IntPtr DataOutPtr { get; }
        public IntPtr Plan       { get; }

        private readonly double[] _dataOut;
        private readonly double[] _window;
        private readonly double   _windowFactor;

        public FFTWTransform(int size) {
            Size          = size;
            _dataOut      = new double[Size / 2];
            DataInPtr     = FFTW.fftw_alloc_real(Size);
            DataOutPtr    = FFTW.fftw_alloc_complex(Size);
            Plan          = FFTW.fftw_plan_dft_r2c(1, ref size, DataInPtr, DataOutPtr, 0);
            _window       = Enumerable.Range(0, Size).Select(n => WindowFunction(n, Size)).ToArray();
            _windowFactor = 1 / Math.Pow(_window.Average(), 2);
        }

        private static double WindowFunction(int n, int m) {
            return 0.5 * (1 - Math.Cos(2 * Math.PI * n / (m - 1)));
        }

        public void UseData(double[] data, int offset, int count) {
            if (count != Size) throw new IndexOutOfRangeException("count must match FFT size");
            Marshal.Copy(data, offset, DataInPtr, count);
            unsafe {
                fixed (double* pWnd = _window) {
                    Core.Multiply_IV64fV64f_IV64f((double*)DataInPtr, pWnd, Size);
                }
            }
        }

        public double[] Transform() {
            FFTW.fftw_execute(Plan);

            unsafe {
                var pOut = (double*)DataOutPtr;
                for (var i = 0; i < _dataOut.Length; i++) {
                    var real = *(pOut + 0);
                    var imag = *(pOut + 1);

                    _dataOut[i] = Math.Sqrt(real * real + imag * imag);

                    pOut += 2;
                }
            }

            // https://www.wavemetrics.com/products/igorpro/dataanalysis/signalprocessing/powerspectra.htm
            Core.Multiply_IV64fS64f_IV64f(_dataOut, 0, 1 * _windowFactor / (double)Size, _dataOut.Length);

            return _dataOut;
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing) {
            if (!disposedValue) {
                if (disposing) {
                    // managed
                }

                // unamanaged
                FFTW.fftw_free(Plan);

                disposedValue = true;
            }
        }

        ~FFTWTransform() {
            Dispose(false);
        }

        public void Dispose() {
            Dispose(true);
        }
        #endregion

    }
}
