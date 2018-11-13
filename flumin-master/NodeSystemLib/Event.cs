using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NodeSystemLib {
    public class Event : IComparable<Event> {

        public TimeStamp Stamp { get; }
        public EventInputPort Target { get; }

        public Event(TimeStamp stamp, EventInputPort port) {
            Stamp = stamp;
            Target = port;
        }

        public int CompareTo(Event other) {
            return Stamp.CompareTo(other.Stamp);
        }
    }

    class EventComparer : IComparer<Event> {

        public int Compare(Event x, Event y) {
            return x.Stamp.CompareTo(y.Stamp);
        }

    }
}
