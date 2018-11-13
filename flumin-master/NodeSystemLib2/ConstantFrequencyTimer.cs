using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace NodeSystemLib2 {

    class ConstantFrequencyTimer : IDisposable {

        readonly Timer _timer;
        TimeSpan _period;
        bool _enabled;
        DateTime _startAt;
        int _tickCount;

        public event EventHandler<TimeSpan> Elapsed;

        public ConstantFrequencyTimer() {
            _timer = new Timer(Callback, null, Timeout.Infinite, Timeout.Infinite);
        }

        public bool Enabled {
            get {
                return _enabled;
            }
            set {
                if (_enabled == value) return;
                lock (this) {
                    if (value) {
                        _timer.Change(_period, _period);
                        _startAt = DateTime.Now;
                    } else {
                        _timer.Change(Timeout.Infinite, Timeout.Infinite);
                    }
                    _enabled = value;
                }
            }
        }

        public TimeSpan Period {
            get {
                return _period;
            }
            set {
                _period = value;
                if (Enabled) {
                    lock (this) {
                        _timer.Change(_period, _period);
                        _startAt = DateTime.Now;
                        _tickCount = 0;
                    }
                }
            }
        }

        private void Callback(object state) {
            lock (this) {
                _tickCount++;

                Elapsed?.Invoke(this, TimeSpan.FromMilliseconds(_tickCount * Period.TotalMilliseconds));

                var elapsed = DateTime.Now - _startAt;

                var millisShouldBe = _tickCount * Period.TotalMilliseconds;
                var millisIs = elapsed.TotalMilliseconds;
                var timeDiff = millisShouldBe - millisIs;
                if (timeDiff > Period.TotalMilliseconds) timeDiff = Period.TotalMilliseconds;
                if (timeDiff < -Period.TotalMilliseconds) timeDiff = -Period.TotalMilliseconds;

                if (_enabled) {
                    _timer.Change(
                        (int)(Period.TotalMilliseconds + timeDiff),
                        (int)Period.TotalMilliseconds
                    );
                }
            }
        }

        public void Dispose() {
            Enabled = false;
            _timer.Dispose();
        }
    }

}
