using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NodeSystemLib2.FileFormats {
    public class Stream2DWriter : IDisposable {

        private TextWriter _writer;

        public static readonly string[] Columns = { "value", "time" };

        public string Delimiter { get; }

        public Stream2DWriter(TextWriter writer, char delimiter) {
            _writer = writer;
            Delimiter = $"{delimiter}";
            InitializeStream();
        }

        public void WriteSample(NodeSystemLib2.FormatValue.TimeLocatedValue<double> value) {
            var timeString  = value.Stamp.ToShortTimeString();
            var valueString = value.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);

            _writer.Write(timeString);
            _writer.Write(Delimiter);
            _writer.WriteLine(valueString);
        }

        private void InitializeStream() {
            _writer.WriteLine(string.Join(Delimiter, Columns));
        }

        public void Dispose() {
            _writer?.Flush();
            _writer?.Close();
            _writer?.Dispose();
            _writer = null;
        }
    }
}
