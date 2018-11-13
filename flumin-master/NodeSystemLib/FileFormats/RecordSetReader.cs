using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NodeSystemLib.FileFormatLib {
    public class RecordSetReader : IDisposable {

        private TextReader _reader;
        private RecordSet _set;

        private string _currentLine;

        public RecordSet Set => _set;

        public RecordSetReader(TextReader reader) {
            _reader = reader;
            _set = new RecordSet(parent: null);
            _currentLine = _reader.ReadLine();
            ReadAll();
        }

        private void ReadAll() {
            if (!LineMatch(RecordSetWriter.Magic)) {
                throw new InvalidDataException("Invalid format");
            }

            while (LineMatch(RecordSetWriter.RecordBegin)) {
                var record = new Record();

                while (LineMatch(RecordSetWriter.LineBegin)) {
                    var props = new Dictionary<string, string>();

                    while (LinePeek().StartsWith("-", StringComparison.Ordinal)) {
                        var line = LineConsume().Substring(1);
                        var fields = line.Split(new [] { ':' }, 2);
                        props.Add(fields[0].Trim(), fields[1].Trim());
                    }

                    switch (props["Type"]) {
                        case "Stream1D":
                            record.Lines.Add(new RecordLineStream1D(props));
                            break;
                        case "Stream2D":
                            record.Lines.Add(new RecordLineStream2D(props));
                            break;
                        default:
                            throw new NotImplementedException("Unknown stream type: " + props["Type"]);
                    }
                }

                _set.AddRecord(record);
            }
        }

        private string LinePeek() => _currentLine ?? "";

        private string LineConsume() {
            var line = _currentLine;
            _currentLine = _reader.ReadLine();
            return line;
        }

        private bool LineMatch(string match) {
            if (_currentLine == match) {
                _currentLine = _reader.ReadLine();
                return true;
            }
            return false;
        }

        public void Dispose() {
            _reader?.Dispose();
            _reader = null;
        }
    }
}
