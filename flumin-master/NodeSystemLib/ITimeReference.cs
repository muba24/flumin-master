using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NodeSystemLib {
    interface ITimeReference<T> where T : IComparable<T> {
        TimeSpan GetTimeSpan(T time);
    }
}
