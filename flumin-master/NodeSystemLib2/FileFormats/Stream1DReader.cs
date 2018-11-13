using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NodeSystemLib2.FileFormats {
    public class Stream1DReader : IDisposable {

        BinaryReader _inner;

        public Stream1DReader(BinaryReader inner) {
            _inner = inner;
        }

        /// <summary>
        /// Total amount of samples in the stream
        /// </summary>
        /// <exception cref="NotSupportedException">Underlying stream does not support Length property</exception>
        public long SampleCount => _inner.BaseStream.Length / sizeof(double);

        /// <summary>
        /// Position in the stream measured in samples
        /// </summary>
        public long Position => _inner.BaseStream.Position / sizeof(double);

        /// <summary>
        /// Seek to a sample in the stream
        /// </summary>
        /// <param name="offset">offset of samples, not bytes</param>
        /// <param name="origin"></param>
        /// <returns></returns>
        public long Seek(long offset, SeekOrigin origin) {
            return _inner.BaseStream.Seek(offset * sizeof(double), origin);
        }

        /// <summary>
        /// Reads samples from binary formatted stream
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        /// <returns>number of samples read. returns 0 if you try to read starting from the end of the stream</returns>
        /// <exception cref="IOException">Possibly thrown if there was a hardware error</exception>
        public long ReadSamples(double[] buffer, long offset, long count) {
            long i = offset;

            if (SampleCount - Position < count) {
                count = SampleCount - Position;
            }

            try {
                for (; i < offset + count; i++) {
                    buffer[i] = _inner.ReadDouble();
                }
            } catch (EndOfStreamException) { }

            return i - offset;
        }

        public void Dispose() {
            _inner?.Dispose();
            _inner = null;
        }
    }
}
