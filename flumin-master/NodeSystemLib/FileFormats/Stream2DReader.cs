﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NodeSystemLib;

namespace NodeSystemLib {
    namespace FileFormatLib {

        public class Stream2DReader : IDisposable {

            TextReader _reader;

            public string Delimiter { get; }

            /// <summary>
            /// Constructor reads a line from the stream and expects it to be the CSV header
            /// with the correct column titles
            /// </summary>
            /// <param name="reader"></param>
            /// <param name="delim">CSV delimiter</param>
            /// <exception cref="InvalidDataException">Thrown if data has wrong format</exception>
            public Stream2DReader(TextReader reader, char delim) {
                _reader = reader;
                Delimiter = $"{delim}";
                if (!InitStream()) throw new InvalidDataException("Wrong data format?");
            }

            /// <summary>
            /// Read a single sample
            /// </summary>
            /// <returns></returns>
            /// <exception cref="IOException">Possibly thrown for hardware errors</exception>
            /// <exception cref="OutOfMemoryException">No memory left</exception>
            /// <exception cref="ArgumentOutOfRangeException">Line too big for buffer to store</exception>
            public TimeLocatedValue ReadSample() {
                var line = _reader.ReadLine();
                if (line == null) return null;

                var fields = line.Split(Delimiter[0]);
                var result = new TimeLocatedValue(double.Parse(fields[1]), TimeStamp.ParseShortTimeString(fields[0]));

                return result;
            }

            /// <summary>
            /// reads the title row and checks for the proper amount and order of columns
            /// </summary>
            /// <returns>true if columns were identified correctly</returns>
            private bool InitStream() {
                var line =_reader.ReadLine();
                if (line == null) return false;

                var header = line.Split(Delimiter[0]);

                if (header.Length != Stream2DWriter.Columns.Length) return false;

                for (int i = 0; i < header.Length; i++) {
                    if (!header[i].Equals(Stream2DWriter.Columns[i])) return false;
                }

                return true;
            }

            public void Dispose() {
                _reader?.Dispose();
                _reader = null;
            }
        }

    }
}