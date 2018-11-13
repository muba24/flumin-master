using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NodeSystemLib {
    namespace FileFormatLib {
        public class Stream1DWriter : IDisposable {

            BinaryWriter _writer;

            public long SamplesWritten { get; private set; }

            public Stream1DWriter(BinaryWriter writer) {
                _writer = writer;
            }

            /// <summary>
            /// Writes a buffer of doubles to a binary formatted stream
            /// </summary>
            /// <param name="sample">single sample</param>
            /// <exception cref="IOException">When write fails</exception>
            public void WriteSample(double sample) {
                _writer.Write(sample);
            }

            public void Dispose() {
                _writer?.Flush();
                _writer?.Close();
                _writer?.Dispose();
                _writer = null;
            }
        }
    }
}