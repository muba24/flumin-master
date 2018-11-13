using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NodeSystemLib2;

namespace Flumin {

    static class RecordSetUtils {

        public static readonly string HeaderInfo = "+!+ RECORD SET +!+";

        public static void CreateNewFile(string file) {
            using (var writer = System.IO.File.AppendText(file)) {
                writer.WriteLine(HeaderInfo);
            }
        }

        public static bool IsValidRecordSet(string file) {
            var lines = System.IO.File.ReadAllLines(file);
            return lines[0] == HeaderInfo;
        }

        public static TimeStamp ParseTimeStamp(string stamp) {
            var parts = stamp.Split(':');

            var hours = int.Parse(parts[0]);
            var mins  = int.Parse(parts[1]);
            var secs  = int.Parse(parts[2]);
            var msecs = int.Parse(parts[3]);

            var samplerate = 10000000L;
            var totalMilliseconds = msecs + secs * 1000L + mins * 60L * 1000L + hours * 60L * 60L * 1000L;
            var samples = totalMilliseconds * samplerate / 1000L;

            return new TimeStamp(samples, (int)samplerate);
        }

        public static string CreateTimeStamp(TimeStamp stamp) {
            var totalMilliseconds = (long)(stamp.AsSeconds() * 1000);

            var hours       = totalMilliseconds / 1000 / 60 / 60;
            var minutes     = (totalMilliseconds - hours * 60 * 60 * 1000) / 1000 / 60;
            var seconds     = (totalMilliseconds - minutes * 60 * 1000) / 1000;
            var mseconds    = totalMilliseconds % 1000;

            return string.Format("{0:00}:{1:00}:{2:00}:{3:000}", hours, minutes, seconds, mseconds);
        }

    }

    static class RecordSetReader {

        public static Recording[] ReadRecords(string file) {
            if (!RecordSetUtils.IsValidRecordSet(file)) {
                throw new FormatException("Invalid file format for set");
            }

            return System.IO.File.ReadAllLines(file)
                                 .Skip(1)
                                 .Select(line => ParseRecordLine(line))
                                 .ToArray();
        }

        private static Recording ParseRecordLine(string line) {
            var parts = line.Split('\t');
            var srate = int.Parse(parts[1]);
            var begin = RecordSetUtils.ParseTimeStamp(parts[2]);
            var end   = RecordSetUtils.ParseTimeStamp(parts[3]);
            var rec   = new Recording(parts[0], srate, begin, end);

            return rec;
        }

    }

    class RecordSetWriter {

        public string File { get; }

        public RecordSetWriter(string file) {
            if (!System.IO.File.Exists(file)) {
                RecordSetUtils.CreateNewFile(file);
            } else {
                if (!RecordSetUtils.IsValidRecordSet(file)) {
                    throw new FormatException("Invalid file format for set");
                }
            }

            File = file;
        }

        public void AddRecord(Recording record) {
            var line = $"{record.Filename}\t{record.Samplerate}\t{RecordSetUtils.CreateTimeStamp(record.Begin)}\t{RecordSetUtils.CreateTimeStamp(record.End)}";
            System.IO.File.AppendAllLines(File, new string[] { line });
        }

    }

}
