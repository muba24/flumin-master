using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flumin {

    public class ConcurrentTreeBag<T> : C5.TreeBag<T> {

        private readonly object _lockEnq = new object();

        public ConcurrentTreeBag(IComparer<T> comp) : base(comp) {
        }

        // the concurrent tree bag can be seen as a priority queue,
        // therefore it has enqueue and dequeue methods.
        // Also this should actually have the 'safe' prefix.
        // TODO: Maybe refactor?
        public bool TryDequeue(out T a) {
            lock (_lockEnq) {
                a = this[0];
                Remove(a);
                return true;
            }
        }

        public void SafeEnqueue(T obj) {
            lock (_lockEnq) {
                Add(obj);
            }
        }

        public void SafeClear() {
            lock (_lockEnq) {
                Clear();
            }
        }

        public bool SafeTryWeakPredecessor(T a, out T b) {
            lock (_lockEnq) {
                return TryWeakPredecessor(a, out b);
            }
        }

        public void SafeRemoveRangeTo(T a) {
            lock (_lockEnq) {
                RemoveRangeTo(a);
            }
        }

        public ConcurrentTreeBag<T> Clone() {
            var copy = new ConcurrentTreeBag<T>(Comparer);
            copy.AddAll(this.Snapshot());
            return copy;
        }

    }

}
