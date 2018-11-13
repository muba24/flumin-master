using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NodeSystemLib2.FileFormats {

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
                string value = tuple.Value;
                if (tuple.Key == "Path") {
                    var idxdir = PathAddBackslash(Path.GetDirectoryName(((FileStream)((StreamWriter)_writer).BaseStream).Name));
                    value = MakeRelativePath(idxdir, value);
                }
                _writer.WriteLine($"-{tuple.Key}: {value}");
            }
        }

        private void Initialize() {
            _writer.WriteLine(Magic);
            _state = State.Main;
        }

        public void Dispose() {
            _writer?.Flush();
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


        // http://stackoverflow.com/a/340454
        private static String MakeRelativePath(String fromPath, String toPath) {
            if (String.IsNullOrEmpty(fromPath)) throw new ArgumentNullException("fromPath");
            if (String.IsNullOrEmpty(toPath)) throw new ArgumentNullException("toPath");

            Uri fromUri = new Uri(fromPath);
            Uri toUri = new Uri(toPath);

            if (fromUri.Scheme != toUri.Scheme) { return toPath; } // path can't be made relative.

            Uri relativeUri = fromUri.MakeRelativeUri(toUri);
            String relativePath = Uri.UnescapeDataString(relativeUri.ToString());

            if (toUri.Scheme.Equals("file", StringComparison.InvariantCultureIgnoreCase)) {
                relativePath = relativePath.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
            }

            return relativePath;
        }


        // http://stackoverflow.com/a/20406065
        private string PathAddBackslash(string path) {
            // They're always one character but EndsWith is shorter than
            // array style access to last path character. Change this
            // if performance are a (measured) issue.
            string separator1 = Path.DirectorySeparatorChar.ToString();
            string separator2 = Path.AltDirectorySeparatorChar.ToString();

            // Trailing white spaces are always ignored but folders may have
            // leading spaces. It's unusual but it may happen. If it's an issue
            // then just replace TrimEnd() with Trim(). Tnx Paul Groke to point this out.
            path = path.TrimEnd();

            // Argument is always a directory name then if there is one
            // of allowed separators then I have nothing to do.
            if (path.EndsWith(separator1) || path.EndsWith(separator2))
                return path;

            // If there is the "alt" separator then I add a trailing one.
            // Note that URI format (file://drive:\path\filename.ext) is
            // not supported in most .NET I/O functions then we don't support it
            // here too. If you have to then simply revert this check:
            // if (path.Contains(separator1))
            //     return path + separator1;
            //
            // return path + separator2;
            if (path.Contains(separator2))
                return path + separator2;

            // If there is not an "alt" separator I add a "normal" one.
            // It means path may be with normal one or it has not any separator
            // (for example if it's just a directory name). In this case I
            // default to normal as users expect.
            return path + separator1;
        }

    }

}
