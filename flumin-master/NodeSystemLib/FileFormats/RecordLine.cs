using NodeSystemLib;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NodeSystemLib {
    namespace FileFormatLib {

        public abstract class RecordLine {

            public const string DateFormat = "yyyy-MM-dd HH:mm";

            public enum LineType {
                Stream1D,
                Stream2D
            }

            public LineType Type { get; }
            public DateTime Date { get; }
            public TimeStamp Begin { get; }
            public TimeStamp End { get; }
            public string Path { get; }

            public RecordLine(Dictionary<string, string> properties) {
                Type  = (LineType)Enum.Parse(typeof(LineType), properties["Type"]);
                Date  = DateTime.ParseExact(properties["Date"], DateFormat, CultureInfo.InvariantCulture);
                Begin = TimeStamp.ParseShortTimeString(properties["Begin"]);
                End   = TimeStamp.ParseShortTimeString(properties["End"]);
                Path  = properties["Path"];
            }

            public RecordLine(LineType type, DateTime date, TimeStamp begin, TimeStamp end, string path) {
                Type = type;
                Date = date;
                Begin = begin;
                End = end;
                Path = path;
            }

            public virtual Dictionary<string, string> GetProperties() {
                return new Dictionary<string, string>() {
                    { "Type", Type.ToString() },
                    { "Date", Date.ToString(DateFormat) },
                    { "Begin", Begin.ToShortTimeString() },
                    { "End", End.ToShortTimeString() },
                    { "Path", Path }
                };
            }

        }

        public class RecordLineStream1D : RecordLine {

            public int Samplerate { get; }

            public RecordLineStream1D(Dictionary<string, string> properties) : base(properties) {
                Samplerate = int.Parse(properties["Samplerate"]);
            }

            public RecordLineStream1D(DateTime date, TimeStamp begin, TimeStamp end, string path, int samplerate)
                : base(LineType.Stream1D, date, begin, end, path) {

                Samplerate = samplerate;
            }

            public override Dictionary<string, string> GetProperties() {
                var values = base.GetProperties();
                values.Add("Samplerate", Samplerate.ToString());
                return values;
            }


        }

        public class RecordLineStream2D : RecordLine {

            public RecordLineStream2D(Dictionary<string, string> properties) : base(properties) {
                //
            }

            public RecordLineStream2D(DateTime date, TimeStamp begin, TimeStamp end, string path)
                : base(LineType.Stream2D, date, begin, end, path) { }


        }

    }
}