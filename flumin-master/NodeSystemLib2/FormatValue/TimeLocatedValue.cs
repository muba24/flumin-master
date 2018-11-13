using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NodeSystemLib2.FormatValue {

    public struct TimeLocatedValue<T> {

        public TimeStamp Stamp { get; }
        public T Value { get; }

        public TimeLocatedValue(T value, TimeStamp stamp) {
            Stamp = stamp;
            Value = value;
        }

    }

}
