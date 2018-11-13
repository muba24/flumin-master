using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleADC.NodeSystem {

    [TypeConverter(typeof(ExpandableObjectConverter))]
    public class TimeLocatedValue {
        public TimeStamp Stamp { get; private set; }
        public double Value { get; set; }

        public void SetStamp(TimeStamp stmp) {
            Stamp = stmp;
        }

        public TimeLocatedValue(double value, TimeStamp stamp) {
            Value = value;
            Stamp = stamp;
        }
    }

    public class TimeLocatedValueComparer : IComparer<TimeLocatedValue> {

        public int Compare(TimeLocatedValue x, TimeLocatedValue y) {
            return (x.Stamp.Value == y.Stamp.Value) ? 0 :
                   (x.Stamp.Value < y.Stamp.Value) ? -1 :
                                                      1 ;  
        }

    }

}
