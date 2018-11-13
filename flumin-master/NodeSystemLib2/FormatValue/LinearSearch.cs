using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NodeSystemLib2.FormatValue {

    public class LinearSearch<T, K> where K : struct {

        private readonly CustomSortedList<T, K> _parent;
        private T _currentItem;
        private int _index;

        public LinearSearch(CustomSortedList<T, K> parent, K startKey) {
            if (parent == null) throw new ArgumentNullException();
            _parent = parent;

            // possible outcomes for binarysearch:
            // index >= 0: meaning, start there
            // index < 0: start at ~index

            _index = parent.BinarySearch(0, parent.Count, startKey);
            if (_index < 0) _index = ~_index;

            if (_index >= _parent.Count) {
                if (parent.Count == 0) {
                    _currentItem = default(T);
                } else {
                    _currentItem = _parent[parent.Count - 1];
                }
            } else {
                var cmp = _parent.CompareFunction(startKey, _parent[_index]);
                if (cmp < 0) {
                    if (_index == 0) {
                        _currentItem = default(T);
                    } else {
                        _index--;
                        _currentItem = _parent[_index];
                    }
                } else {
                    _currentItem = _parent[_index];
                }
            }
        }

        public T CurrentItem => _currentItem;

        public bool Advance(K to) {
            if (_index >= _parent.Count) return true;
            if (_parent.CompareFunction(to, _parent[_index]) < 0) return true;

            _currentItem = _parent[_index];

            for (; _index < _parent.Count - 1; _index++) {
                var nextItem = _parent[_index + 1];
                var cmp = _parent.CompareFunction(to, nextItem);
                if (cmp < 0) {
                    _currentItem = _parent[_index];
                    break;
                }
                _currentItem = nextItem;
            }
            return true;
        }

    }

}
