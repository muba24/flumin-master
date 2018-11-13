using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace NodeSystemLib {
    class TimedTaskQueue<T> : IDisposable where T : IComparable<T> {

        class Entry : IComparable<Entry> {
            public T Stamp;
            public Action Action;

            public int CompareTo(Entry other) {
                return Stamp.CompareTo(other.Stamp);
            }
        }

        private readonly List<Entry> _actions = new List<Entry>();
        private readonly object _cancelLock = new object();

        private readonly ITimeReference<T> _timeRef;

        private CancellationTokenSource _waitCancelSource;
        private CancellationToken _waitCancelToken;

        private CancellationTokenSource _threadCancelSource;
        private CancellationToken _threadCancelToken;

        private Thread _workThread;

        public TimedTaskQueue(ITimeReference<T> timeRef) {
            _timeRef = timeRef;
            RunThread();
        }

        public void AddAction(T when, Action action) {
            lock (_actions) {
                var newItem = new Entry { Stamp = when, Action = action };
                var index = _actions.BinarySearch(newItem);
                if (index < 0) index = ~index;
                _actions.Insert(index, newItem);
            }

            lock (_cancelLock) {
                _waitCancelSource.Cancel();
            }
        }

        public void CancelAll() {
            lock (_actions) {
                _actions.Clear();
            }
            _waitCancelSource.Cancel();
        }

        public void WaitForTasks() {
            while (_actions.Count > 0) {
                Thread.Sleep(100);
            }
        }

        private void RunThread() {
            ResetWaitCancelToken();

            _threadCancelSource = new CancellationTokenSource();
            _threadCancelToken = _threadCancelSource.Token;

            _workThread = new Thread(WaitThread);
            _workThread.Start();
        }

        private void ResetWaitCancelToken() {
            lock (_cancelLock) {
                _waitCancelSource = new CancellationTokenSource();
                _waitCancelToken = _waitCancelSource.Token;
            }
        }

        private static bool Wait(int millisecondsDelay, CancellationToken cancelToken) {
            try {
                var handle = Task.Delay(millisecondsDelay, cancelToken);
                handle.Wait();
                return true;
            } catch (AggregateException ex) when (ex.InnerException is TaskCanceledException) {
                return false;
            } catch (TaskCanceledException) {
                return false;
            }
        }

        private void WaitThread() {
            while (!_threadCancelToken.IsCancellationRequested) {
                while (_actions.Count == 0) {
                    if (!Wait(10000, _waitCancelToken)) {
                        if (_threadCancelToken.IsCancellationRequested) return;
                        ResetWaitCancelToken();
                    }
                }

                Entry item = null;

                while (_actions.Count > 0) {
                    lock (_actions) {
                        if (_actions.Count == 0) break;
                        item = _actions[0];
                    }

                    while (true) {
                        if (_timeRef.GetTimeSpan(item.Stamp).TotalMilliseconds <= 10) break;
                        var delay = Math.Max(0, (int)_timeRef.GetTimeSpan(item.Stamp).TotalMilliseconds);

                        if (!Wait(delay / 2, _waitCancelToken)) {
                            if (_threadCancelToken.IsCancellationRequested) return;
                            ResetWaitCancelToken();
                            item = null;
                            break;
                        }
                    }

                    lock (_actions) {
                        if (item == null) continue;

                        var entry = _actions[0];
                        _actions.RemoveAt(0);

                        while (_timeRef.GetTimeSpan(entry.Stamp).TotalMilliseconds > 1) {
                            // wait burst
                        }

                        if (_threadCancelToken.IsCancellationRequested) return;
                        entry.Action();
                    }
                }

            }
        }

        public void Dispose() {
            _waitCancelSource.Cancel();
            _threadCancelSource.Cancel();
        }
    }
}
