using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NodeSystemLib {

    public struct TimeStamp : IComparable<TimeStamp> {

        private const double SmallestPeriodLength = 10E-7 * 1000; // in ms

        private double _stamp;

        public TimeStamp(TimeStamp cpy) {
            _stamp = cpy._stamp;
        }

        public TimeStamp(double ms) {
            _stamp = ms;
        }

        public TimeStamp(long samples, long rate) {
            _stamp = samples * 1000.0 / rate;
        }

        public override string ToString() {
            return Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        public string ToShortTimeString() {
            var totalMilliseconds = (long)(AsSeconds() * 1000);

            var hours       = totalMilliseconds / 1000 / 60 / 60;
            var minutes     = (totalMilliseconds - hours * 60 * 60 * 1000) / 1000 / 60;
            var seconds     = (totalMilliseconds - minutes * 60 * 1000) / 1000;
            var mseconds    = totalMilliseconds % 1000;

            return string.Format("{0:00}:{1:00}:{2:00}:{3:000}", hours, minutes, seconds, mseconds);
        }

        public static TimeStamp ParseShortTimeString(string stamp) {
            var parts = stamp.Split(':');

            var hours = int.Parse(parts[0]);
            var mins  = int.Parse(parts[1]);
            var secs  = int.Parse(parts[2]);
            var msecs = int.Parse(parts[3]);

            var samplerate = 10000000L;
            var totalMilliseconds = msecs + secs * 1000L + mins * 60L * 1000L + hours * 60L * 60L * 1000L;
            var samples = totalMilliseconds * samplerate / 1000L;

            return new TimeStamp(samples, samplerate);
        }

        public double Value => _stamp;

        public double AsSeconds() => _stamp / 1000.0;

        public long ToRate(long rate) => (long)(_stamp * rate / 1000.0);

        public TimeStamp AddValue(long samples) {
            var stamp = new TimeStamp(this);
            stamp._stamp += samples;
            return stamp;
        }

        public void AddInplace(long samples, long rate) {
            _stamp = _stamp + samples * 1000.0 / rate;
        }

        public TimeStamp Add(double ms) {
            return new TimeStamp(_stamp + ms);
        }

        public TimeStamp Add(long samples, long rate) {
            return new TimeStamp(Value + samples * 1000.0 / rate);
        }

        public TimeStamp Sub(long samples, long rate) {
            return new TimeStamp(Value - samples * 1000.0 / rate);
        }

        public static TimeStamp Zero() => new TimeStamp(0, 1);

        public static TimeStamp operator-(TimeStamp t1, TimeStamp t2) {
            return new TimeStamp(t1.Value - t2.Value);
        }

        public static bool operator<(TimeStamp t1, TimeStamp t2) {
            return t1.Value < t2.Value;
        }

        public static bool operator>(TimeStamp t1, TimeStamp t2) {
            return t1.Value > t2.Value;
        }

        public static bool operator<=(TimeStamp t1, TimeStamp t2) {
            return t1.Value <= t2.Value;
        }

        public static bool operator>=(TimeStamp t1, TimeStamp t2) {
            return t1.Value >= t2.Value;
        }

        public override bool Equals(object obj) {
            if (obj is TimeStamp) {
                return Math.Abs(((TimeStamp)obj)._stamp - _stamp) < SmallestPeriodLength;
            }
            return base.Equals(obj);
        }

        public int CompareTo(TimeStamp other) {
            //return (this.Value <  other.Value) ? -1 :
            //       (this.Value == other.Value) ?  0 :
            //                                      1;

            return (this.Value < other.Value) ?                             -1 :
                   (Math.Abs(this.Value - other.Value) <= double.Epsilon) ?  0 :
                                                                             1;
        }
    }

    class TimeStampComparer : IComparer<TimeStamp> {
        public int Compare(TimeStamp x, TimeStamp y)
            //=> (x.Value < y.Value)  ? -1 :
            //   (x.Value == y.Value) ?  0 :
            //                           1;

            =>     (x.Value < y.Value) ?                             -1 :
                   (Math.Abs(x.Value - y.Value) <= double.Epsilon) ?  0 :
                                                                      1;
    }

}
