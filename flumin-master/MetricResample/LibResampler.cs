using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace MetricResample {
    class LibResampler : IDisposable {

        [DllImport("libresample.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr resample_open(int highQuality, double minFactor, double maxFactor);

        [DllImport("libresample.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr resample_dup(IntPtr handle);

        [DllImport("libresample.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int resample_get_filter_width(IntPtr handle);

        [DllImport("libresample.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int resample_process(IntPtr handle,
                             double factor,
                             IntPtr inBuffer,
                             int inBufferLen,
                             int lastFlag,
                             ref int inBufferUsed,
                             IntPtr outBuffer,
                             int outBufferLen);

        [DllImport("libresample.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern void resample_close(IntPtr handle);


        // ------------------------------------------------------------------------

        private readonly IntPtr _handle;
        private readonly double _factor;
        private bool _disposed;
        private double[] _input;
        private double[] _output;
        private GCHandle _gcInput;
        private GCHandle _gcOutput;

        private int _inputLen;
        private int _outputLen;

        public LibResampler(double samplerateIn, double samplerateOut, int bufferSizeIn, int bufferSizeOut) {
            if (samplerateIn <= 0) throw new ArgumentException(nameof(samplerateIn));
            if (samplerateOut <= 0) throw new ArgumentException(nameof(samplerateOut));

            _factor = samplerateOut / samplerateIn;

            _handle = resample_open(1, _factor, _factor);
            if (_handle == IntPtr.Zero) {
                throw new ArgumentOutOfRangeException($"Either {nameof(samplerateIn)} or {nameof(samplerateOut)} out of range");
            }

            _input = new double[bufferSizeIn];
            _output = new double[bufferSizeOut];

            _gcInput = GCHandle.Alloc(_input, GCHandleType.Pinned);
            _gcOutput = GCHandle.Alloc(_output, GCHandleType.Pinned);
        }

        public void Dispose() {
            if (!_disposed) {
                _gcInput.Free();
                _gcOutput.Free();

                if (_handle != IntPtr.Zero) {
                    resample_close(_handle);
                }

                _disposed = true;
            }
        }

        public int InputAvailable => _inputLen;
        public int InputFree => _input.Length - _inputLen;

        public int OutputAvailable => _outputLen;
        public int OutputFree => _output.Length - _outputLen;

        public int PutData(double[] data, int offset, int count) {
            count = Math.Min(InputFree, count);
            Array.Copy(data, offset, _input, InputAvailable, count);
            _inputLen += count;
            return count;
        }

        public int GetData(double[] dest, int offset, int count) {
            count = Math.Min(OutputAvailable, count);
            Array.Copy(_output, 0, dest, offset, count);
            _outputLen -= count;
            Array.Copy(_output, count, _output, 0, _outputLen);
            return count;
        }

        public void Flush() {
            int inUsed = 0;
            int outUsed = resample_process(
                _handle,
                _factor,
                _gcInput.AddrOfPinnedObject(),
                InputAvailable,
                1,
                ref inUsed,
                IntPtr.Add(_gcOutput.AddrOfPinnedObject(), OutputAvailable * sizeof(double)),
                OutputFree);

            // can only be thrown if _factor is not in the range specified when creating the handle
            if (outUsed < 0) throw new InvalidOperationException();

            _outputLen += outUsed;
            _inputLen -= inUsed;

            Array.Copy(_input, inUsed, _input, 0, _inputLen);
        }

        public void Resample() {
            int inUsed = 0;
            int outUsed = resample_process(
                _handle,
                _factor,
                _gcInput.AddrOfPinnedObject(),
                InputAvailable,
                0,
                ref inUsed,
                IntPtr.Add(_gcOutput.AddrOfPinnedObject(), OutputAvailable * sizeof(double)),
                OutputFree);

            // can only be thrown if _factor is not in the range specified when creating the handle
            if (outUsed < 0) throw new InvalidOperationException();

            _outputLen += outUsed;
            _inputLen -= inUsed;

            Array.Copy(_input, inUsed, _input, 0, _inputLen);
        }

    }

}
