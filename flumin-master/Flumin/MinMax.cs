using System;

namespace Flumin {

    /// <summary>
    /// Minimum/Maximum window cascade
    /// </summary>

    public struct MinMax {
        public MinMax(double min, double max) {
            Min = min;
            Max = max;
        }

        public double Min { get; }
        public double Max { get; }
    }

    public class MinMaxCacheFirstStage {
        public readonly GenericRingBuffer<MinMax> Samples;

        private readonly double[] _lastSamples;
        private int _lastSampleCount;

        public MinMaxCache NextMinMaxCache { get; set; }

        public MinMaxCacheFirstStage(int factor, int size = 10 * 1024 * 1024) {
            Samples = new GenericRingBuffer<MinMax>(size);
            _lastSamples = new double[factor];
        }

        public void ConsumeSamples(double[] newSamples) {
            var newSamplePos = 0;

            if (newSamples.Length > _lastSamples.Length - _lastSampleCount) {
                Array.Copy(newSamples, 0, _lastSamples, _lastSampleCount, _lastSamples.Length - _lastSampleCount);
                newSamplePos += _lastSamples.Length - _lastSampleCount;
                ConsumeBuffer();
            }

            while (newSamples.Length - newSamplePos >= _lastSamples.Length) {
                Array.Copy(newSamples, newSamplePos, _lastSamples, 0, _lastSamples.Length);
                _lastSampleCount = _lastSamples.Length;

                ConsumeBuffer();
                newSamplePos += _lastSamples.Length;
            }

            if (newSamples.Length - newSamplePos > 0) {
                Array.Copy(newSamples, newSamplePos, _lastSamples, 0, newSamples.Length - newSamplePos);
                _lastSampleCount = newSamples.Length - newSamplePos;
            }
        }

        private void ConsumeBuffer() {
            var min = _lastSamples[0];
            var max = _lastSamples[0];

            for (var j = 1; j < _lastSamples.Length; j++) {
                if (_lastSamples[j] < min) min = _lastSamples[j];
                else if (_lastSamples[j] > max) max = _lastSamples[j];
            }

            var newMinMaxValue = new MinMax(min, max);
            _lastSampleCount = 0;

            if (Samples.Length == Samples.Capacity) Samples.Skip(1);
            Samples.Enqueue(newMinMaxValue);
            NextMinMaxCache?.ConsumeSample(newMinMaxValue);
        }
    }

    public class MinMaxCache {
        public readonly GenericRingBuffer<MinMax> Samples;

        private readonly MinMax[] _lastSamples;
        private int _lastSampleCount;

        public MinMaxCache NextMinMaxCache { get; set; }

        public MinMaxCache(int factor, int size = 10 * 1024 * 1024) {
            Samples = new GenericRingBuffer<MinMax>(size);
            _lastSamples = new MinMax[factor];
        }

        public void ConsumeSample(MinMax sample) {
            _lastSamples[_lastSampleCount++] = sample;
            if (_lastSampleCount != _lastSamples.Length) return;

            var min = _lastSamples[0].Min;
            var max = _lastSamples[0].Max;

            for (var i = 1; i < _lastSamples.Length; i++) {
                min = _lastSamples[i].Min < min ? _lastSamples[i].Min : min;
                max = _lastSamples[i].Max > max ? _lastSamples[0].Max : max;
            }

            var newMinMaxValue = new MinMax(min, max);
            _lastSampleCount = 0;

            if (Samples.Length == Samples.Capacity) Samples.Skip(1);
            Samples.Enqueue(newMinMaxValue);
            NextMinMaxCache?.ConsumeSample(newMinMaxValue);
        }
    }
}