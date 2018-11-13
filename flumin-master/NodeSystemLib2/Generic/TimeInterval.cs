using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NodeSystemLib2.Generic {
    public class TimeInterval : IComparable<TimeInterval> {
        public TimeStamp Begin;
        public TimeStamp End;

        public long ToRate(long samplerate) {
            return (long)(AsSeconds() * samplerate);
        }

        public double AsSeconds() {
            return End.AsSeconds() - Begin.AsSeconds();
        }

        public int ToRate(int samplerate) {
            return (int)(AsSeconds() * samplerate);
        }

        public int CompareTo(TimeInterval other) {
            if (this < other) {
                return 1;
            } else {
                return -1;
            }
        }

        public TimeInterval(TimeStamp begin, TimeStamp end) {
            Begin = begin;
            End = end;
        }

        public static bool operator >(TimeInterval l, TimeInterval r) {
            return (l.End.AsSeconds() - l.Begin.AsSeconds()) > (r.End.AsSeconds() - r.Begin.AsSeconds());
        }

        public static bool operator <(TimeInterval l, TimeInterval r) {
            return (l.End.AsSeconds() - l.Begin.AsSeconds()) < (r.End.AsSeconds() - r.Begin.AsSeconds());
        }
    }
}
