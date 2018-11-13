using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleADC {

    public struct TimeStamp {

        public const long MaxSamplerate = 1000000;

        private long _stamp;

        public TimeStamp(TimeStamp cpy) {
            _stamp = cpy._stamp;
        }

        public TimeStamp(double ms) {
            _stamp = (long)(ms * MaxSamplerate / 1000);
        }

        public TimeStamp(long samples, long rate) {
            _stamp = samples * MaxSamplerate / rate;
        }

        public override string ToString() {
            return Value.ToString();
        }

        public string ToShortTimeString() {
            var totalMilliseconds = (long)(AsSeconds() * 1000);

            var hours       = totalMilliseconds / 1000 / 60 / 60;
            var minutes     = (totalMilliseconds - hours * 60 * 60 * 1000) / 1000 / 60;
            var seconds     = (totalMilliseconds - minutes * 60 * 1000) / 1000;
            var mseconds    = totalMilliseconds % 1000;

            return string.Format("{0:00}:{1:00}:{2:00}:{3:000}", hours, minutes, seconds, mseconds);
        }

        public long Value => _stamp;

        public double AsSeconds() => _stamp / (double)MaxSamplerate;

        public long ToRate(long rate) => _stamp * rate / MaxSamplerate;
        
        public TimeStamp AddValue(long samples) {
            var stamp = new TimeStamp(this);
            stamp._stamp += samples;
            return stamp;
        }

        public void AddInplace(long samples, long rate) {
            _stamp = (ToRate(rate) + samples) * MaxSamplerate / rate;
        }

        public TimeStamp Add(long samples, long rate) {
            return new TimeStamp(ToRate(rate) + samples, rate);
        }

        public TimeStamp Sub(long samples, long rate) {
            return new TimeStamp(ToRate(rate) - samples, rate);
        }

        public static TimeStamp Zero() => new TimeStamp(0, 1);

        public static TimeStamp operator-(TimeStamp t1, TimeStamp t2) {
            return new TimeStamp(t1.Value - t2.Value, MaxSamplerate);
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
                return ((TimeStamp)obj)._stamp == _stamp;
            }
            return base.Equals(obj);
        }

    }

    class TimeStampComparer : IComparer<TimeStamp> {
        public int Compare(TimeStamp x, TimeStamp y)
            => (x.Value < y.Value)  ? -1 :
               (x.Value == y.Value) ?  0 :
                                       1;
    }

}
