using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NodeSystemLib2 {

    public struct TimeStamp : IComparable<TimeStamp> {

        private readonly double _stamp;

        public TimeStamp(double seconds) {
            _stamp = seconds;
        }

        public TimeStamp(long samples, int samplerate) {
            _stamp = samples / (double)samplerate;
        }

        public TimeStamp Increment(double seconds) {
            return new TimeStamp(_stamp + seconds);
        }

        public TimeStamp Increment(TimeSpan span) {
            return new TimeStamp(AsSeconds() + span.TotalSeconds);
        }

        public TimeStamp Increment(long samples, int samplerate) {
            return new TimeStamp(AsSeconds() + samples / (double)samplerate);
        }

        public TimeStamp Decrement(long samples, int samplerate) {
            return new TimeStamp(AsSeconds() - samples / (double)samplerate);
        }

        public TimeSpan AsTimeSpan() => TimeSpan.FromSeconds(_stamp);

        public double AsSeconds() => _stamp;

        public static TimeStamp Zero => new TimeStamp(0);

        public override string ToString() => AsTimeSpan().ToString("c");

        public long ToRate(long rate) => (long)(_stamp * rate);

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

            return new TimeStamp(samples, (int)samplerate);
        }

        public override bool Equals(object obj) {
            if (obj != null && obj is TimeStamp) {
                var ts = (TimeStamp)obj;
                return ts == this;
            }
            return base.Equals(obj);
        }

        public override int GetHashCode() {
            return _stamp.GetHashCode();
        }

        public int CompareTo(TimeStamp other) {
            return _stamp.CompareTo(other._stamp);
        }

        public static bool operator<(TimeStamp t1, TimeStamp t2) {
            return t1._stamp < t2._stamp;
        }

        public static bool operator <=(TimeStamp t1, TimeStamp t2) {
            return t1._stamp <= t2._stamp;
        }

        public static bool operator >=(TimeStamp t1, TimeStamp t2) {
            return t1._stamp >= t2._stamp;
        }

        public static bool operator ==(TimeStamp t1, TimeStamp t2) {
            return Math.Abs(t1._stamp - t2._stamp) < 1 / 10000000.0;
        }

        public static bool operator !=(TimeStamp t1, TimeStamp t2) {
            return Math.Abs(t1._stamp - t2._stamp) > 1 / 10000000.0;
        }

        public static bool operator >(TimeStamp t1, TimeStamp t2) {
            return t1._stamp > t2._stamp;
        }

        public static Generic.TimeInterval operator-(TimeStamp t1, TimeStamp t2) {
            return new Generic.TimeInterval(t2, t1); //TimeSpan.FromSeconds(t1._stamp - t2._stamp);
        }

    }

}
