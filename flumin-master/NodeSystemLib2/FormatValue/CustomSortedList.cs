using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NodeSystemLib2.FormatValue {
    /// <summary>
    /// Sort items in a list by a key property of the item.
    /// Keys can be ambiguous.
    /// Contract: The key of an item must not change while it is stored in the list!
    /// </summary>
    /// <typeparam name="T">Type of the item</typeparam>
    /// <typeparam name="K">Type of the key to sort</typeparam>
    public class CustomSortedList<T, K> where K : struct {


        public delegate int Comparer(K key, T item);
        public delegate K Selector(T item);

        protected readonly List<T> _items;
        protected readonly Comparer _CompareFunction;
        protected readonly Selector _SelectFunction;

        public CustomSortedList(Selector selector, Comparer comparer) {
            _items = new List<T>();
            _CompareFunction = comparer;
            _SelectFunction = selector;
        }

        public int Count => _items.Count;

        public Comparer CompareFunction => _CompareFunction;
        public Selector SelectFunction => _SelectFunction;

        public T ElementAt(int index) {
            if (index < 0) throw new ArgumentOutOfRangeException();
            lock (_items) {
                return _items[index];
            }
        }

        public T this[int i] {
            get {
                if (i < 0) throw new ArgumentOutOfRangeException();
                lock (_items) {
                    return _items[i];
                }
            }
        }

        public LinearSearch<T, K> GetLinearSearch(K start) {
            return new LinearSearch<T, K>(this, start);
        }

        public void Clear() {
            lock (_items) {
                _items.Clear();
            }
        }

        public bool Contains(T item) {
            lock (_items) {
                return _items.Contains(item);
            }
        }

        public T FindOrDefault(K key) {
            lock (_items) {
                var index = BinarySearch(0, _items.Count, key);
                if (index < 0) index = ~index;

                index--;

                if (index < 0) return default(T);
                return _items[index];
            }
        }

        public IEnumerable<T> Iterate(K key) {
            var idx = BinarySearch(0, Count, key);
            if (idx < 0) idx = ~idx;
            //if (idx > 0) idx++;

            for (int i = idx; i < Count; i++) {
                lock (_items) {
                    if (i < Count) {
                        yield return _items[i];
                    } else {
                        yield break;
                    }
                }
            }
        }

        public void Add(T item) {
            lock (_items) {
                var insertIndex = BinarySearch(0, _items.Count, _SelectFunction(item));
                if (insertIndex < 0) insertIndex = ~insertIndex;
                _items.Insert(insertIndex, item);
            }
        }

        public void Remove(T item) {
            lock (_items) {
                _items.Remove(item);
            }
        }

        public bool Dequeue(out T v) {
            lock (_items) {
                if (_items.Count == 0) {
                    v = default(T);
                    return false;
                }

                v = _items[0];
                _items.RemoveAt(0);
                return true;
            }
        }

        public int BinarySearch(int index, int count, K key) {
            int lo = index;
            int hi = index + count - 1;
            int mid;

            while (lo <= hi) {
                mid = (lo + hi) / 2;

                var r = _CompareFunction(key, _items[mid]);

                if (r == 0) return mid;
                if (r < 0) hi = mid - 1;
                else lo = mid + 1;
            }

            return ~lo;
        }

    }
}
