using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace NodeSystemLib2 {

    class PerformanceCounter {

        [DllImport("Kernel32.dll")]
        private static extern bool QueryPerformanceCounter(out long lpPerformanceCount);

        [DllImport("Kernel32.dll")]
        private static extern bool QueryPerformanceFrequency(out long lpFrequency);

        private readonly long    _frequency;
        private readonly double _multiplier = 1.0e9;

        private long _start;
        private bool _running;

        public PerformanceCounter() {
            if (!QueryPerformanceFrequency(out _frequency)) {
                throw new System.ComponentModel.Win32Exception();
            }
        }

        public void Start() {
            if (_running) return;

            if (!QueryPerformanceCounter(out _start)) {
                throw new System.ComponentModel.Win32Exception();
            }

            _running = true;
        }

        public long GetTicks() {
            long current;
            if (!QueryPerformanceCounter(out current)) {
                throw new System.ComponentModel.Win32Exception();
            }
            return current - _start;
        }

        public double TicksToSeconds(long ticks) {
            return ticks / (double)_frequency;
        }

        public void Stop() {
            if (!_running) return;
            _running = false;
        }

    }

}
