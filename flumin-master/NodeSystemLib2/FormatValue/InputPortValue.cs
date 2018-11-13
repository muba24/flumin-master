using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NodeSystemLib2.FormatValue {

    public class InputPortValue<T> : InputPort {

        protected CustomSortedList<TimeLocatedValue<T>, TimeStamp> _values;

        public InputPortValue(Node parent, string name, PortDataType type) : base(parent, name, type) {
            _values = new CustomSortedList<TimeLocatedValue<T>, TimeStamp>(
                (v) => v.Stamp,
                (k, v) => k.CompareTo(v.Stamp)
            );
        }

        public void PrepareProcessing() {
            _values.Clear();
        }

        public void Write(TimeLocatedValue<T> value) {
            _values.Add(value);
        }

        public int Count => _values.Count;

        public bool TryDequeue(out TimeLocatedValue<T> v) {
            return _values.Dequeue(out v);
        }

        public bool GetLatestValue(out TimeLocatedValue<T> val) {
            if (_values.Count > 0) {
                val = _values[_values.Count - 1];
                return true;
            }
            val = new TimeLocatedValue<T>();
            return false;
        }

        public bool GetExistentValueAt(TimeStamp stamp, out TimeLocatedValue<T> val) {
            var idx = _values.BinarySearch(0, _values.Count, stamp);
            if (idx < 0) idx = ~idx;
            idx--;

            if (idx < 0 || idx >= _values.Count) {
                val = new TimeLocatedValue<T>();
                return false;
            }

            val = _values[idx];
            return true;
        }

        public void RemoveTill(TimeStamp key) {
            throw new NotImplementedException();
        }

        public IEnumerable<TimeLocatedValue<T>> GetValueIterator(TimeStamp key) {
            return _values.Iterate(key);
        }

        public LinearSearch<TimeLocatedValue<T>, TimeStamp> GetIterator(TimeStamp key) {
            return _values.GetLinearSearch(key);
        }

    }

}
