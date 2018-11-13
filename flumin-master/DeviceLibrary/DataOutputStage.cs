using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeviceLibrary {

    class DataOutputStage {

        private DataOutputBuffer[] _buffers;
        private int _bufferIndex = 0;
        private int _prebufferCount = 0;
        private bool _firstBuffer = true;

        public DataOutputStage(int channels, int samplesPerChannel, int buffers, int prebufferCount) {
            _buffers = new DataOutputBuffer[buffers];
            for (int i = 0; i < buffers; i++) {
                _buffers[i] = new DataOutputBuffer(channels, samplesPerChannel);
            }
            _prebufferCount = prebufferCount;
        }

        public bool BufferReady {
            get {
                if (_firstBuffer) {
                    for (int i = 0; i < _prebufferCount; i++) {
                        if (!_buffers[(_bufferIndex + i) % _buffers.Length].Full) return false;
                    }
                    return true;
                }
                return _buffers[_bufferIndex].Full;
            }
        }

        public DataOutputBuffer CurrentBuffer => _buffers[_bufferIndex];

        public void MoveToNextBuffer() {
            if (!BufferReady) throw new InvalidOperationException("Current buffer is not ready");
            CurrentBuffer.Reset();
            _bufferIndex = (_bufferIndex + 1) % _buffers.Length;
            System.Diagnostics.Debug.WriteLine("Output stage: Move to next buffer: " + _bufferIndex);
            _firstBuffer = false;
        }

        public void Write(int channel, double[] data, int offset, int count) {
            for (int i = 0; count > 0 && i < _buffers.Length; i++) {
                var written = _buffers[_bufferIndex + i].Write(data, offset, count, channel);
                count -= written;
                offset += written;
            }

            if (count > 0) {
                throw new InsufficientMemoryException();
            }
        }

    }

}
