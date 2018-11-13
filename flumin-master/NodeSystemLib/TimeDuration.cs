using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NodeSystemLib {

    public struct TimeDuration {
        public TimeStamp Begin;
        public TimeStamp End;

        public long ToRate(long samplerate) {
            return (End - Begin).ToRate(samplerate);
        }

        public double AsSeconds() {
            return (End - Begin).AsSeconds();
        }

        public TimeDuration(TimeStamp begin, TimeStamp end) {
            Begin = begin;
            End = end;
        }

        public static bool operator >(TimeDuration l, TimeDuration r) {
            return (l.End - l.Begin).Value > (r.End - r.Begin).Value;
        }

        public static bool operator <(TimeDuration l, TimeDuration r) {
            return (l.End - l.Begin).Value < (r.End - r.Begin).Value;
        }
    }

}
