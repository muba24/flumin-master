using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NodeSystemLib {
    namespace FileFormatLib {

        public class RecordSetWriter : IDisposable {

            private enum State {
                Undefined,
                Main,
                Record,
                Line
            }

            public static readonly string Magic = "-+-+-RECORDSET-+-+-";
            public static readonly string RecordBegin = "RECORD";
            public static readonly string LineBegin = "LINE";

            private TextWriter _writer;

            private State _state;

            public RecordSetWriter(TextWriter writer) {
                _writer = writer;
                _state = State.Undefined;
            }

            public void BeginWrite() {
                if (_state != State.Undefined) throw new InvalidOperationException("Invalid in current state");
                Initialize();
            }

            public void EndWrite() {
                if (_state != State.Main) throw new InvalidOperationException("Invalid in current state");
                _writer.Dispose();
                _writer = null;
                _state = State.Undefined;
            }

            public void BeginRecord() {
                if (_state != State.Main) throw new InvalidOperationException("Invalid in current state");
                _state = State.Record;
                _writer.WriteLine(RecordBegin);
            }

            public void EndRecord() {
                if (_state != State.Record) throw new InvalidOperationException("Invalid in current state");
                _state = State.Main;
            }

            public void WriteRecordLine(RecordLine line) {
                if (_state != State.Record) throw new InvalidOperationException("Invalid in current state");
                _writer.WriteLine(LineBegin);
                foreach (var tuple in line.GetProperties()) {
                    _writer.WriteLine($"-{tuple.Key}: {tuple.Value}");
                }
            }

            private void Initialize() {
                _writer.WriteLine(Magic);
                _state = State.Main;
            }

            public void Dispose() {
                _writer?.Dispose();
                _writer = null;
            }

            public static void WriteToFile(RecordSet set, string path) {
                if (System.IO.File.Exists(path)) {
                    System.IO.File.Delete(path);
                }

                using (var writer = new RecordSetWriter(System.IO.File.CreateText(path))) {
                    writer.BeginWrite();
                    foreach (var record in set.Records) {
                        writer.BeginRecord();
                        foreach (var line in record.Lines) {
                            writer.WriteRecordLine(line);
                        }
                        writer.EndRecord();
                    }
                    writer.EndWrite();
                }
            }

        }

    }
}