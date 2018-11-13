using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DeviceLibrary {
    public class PrecisionTimer {

        public delegate void ElapsedHandler(object sender, int elapsed);
        public event ElapsedHandler Elapsed;

        public delegate void TriggerMissedHandler(object sender, int overshoot);
        public event TriggerMissedHandler TriggerMissed;

        private Thread _clockThread;

        private CancellationToken _cancelToken;
        private CancellationTokenSource _cancelTokenSource;
        private bool _enabled;

        public int Milliseconds { get; set; }

        public int ToleranceMilliseconds { get; set; }

        public bool Enabled
        {
            get
            {
                return _enabled;
            }
            set
            {
                if (_enabled == value) return;
                if (value) {
                    Start();
                } else {
                    Stop();
                }
                _enabled = value;
            }
        }

        private void Start() {
            _clockThread?.Join();

            _cancelTokenSource = new CancellationTokenSource();
            _cancelToken = _cancelTokenSource.Token;
            _clockThread = new Thread(ClockThread);
            _clockThread.Start();
        }

        private void Stop() {
            _cancelTokenSource.Cancel();
        }

        private void ClockThread() {
            var watch = new Stopwatch();
            var accum = 0;
            var aim = accum + Milliseconds;

            watch.Start();

            while (true) {
                // Zeit bis Auslösung
                var delta = (int)(aim - watch.ElapsedMilliseconds);

                // Zeit in angemessenem Bereich
                if (delta < ToleranceMilliseconds) {
                    Elapsed?.Invoke(this, (int)(watch.ElapsedMilliseconds - accum));

                    // verpasst
                    if (-delta > ToleranceMilliseconds) {
                        TriggerMissed?.Invoke(this, -delta);
                    }

                    accum += Milliseconds;
                    aim = accum + Milliseconds;
                } else {
                    var wait = delta / 2;
                    Thread.Sleep(wait > 10 ? wait : 0);
                }

                try {
                    _cancelToken.ThrowIfCancellationRequested();
                } catch (OperationCanceledException) {
                    return;
                }
            }
        }
    }
}
