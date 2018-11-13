using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NodeSystemLib2;
using System.IO;

namespace NewOpenGLRenderer {

    /// <summary>
    /// Minimum/Maximum window cascade
    /// </summary>

    public struct MinMax {
        public float Min;
        public float Max;
    }

    public interface IMinMaxCache {
        int Factor { get; }
        GenericRingBuffer<MinMax> Samples { get; }
        IMinMaxCache NextMinMaxCache { get; set; }
        void Clear();
    }

    public class MinMaxCacheFirstStage : IMinMaxCache {

        private int   _lastSampleCount;
        private float _min;
        private float _max;

        public GenericRingBuffer<MinMax> Samples { get; }

        public IMinMaxCache NextMinMaxCache { get; set; }

        public int Factor { get; private set; }

        public MinMaxCacheFirstStage(int factor, int size) {
            Samples = new GenericRingBuffer<MinMax>(size) { FixedSize = true };
            _max = float.MinValue;
            _min = float.MaxValue;
            Factor = factor;
        }

        public void ConsumeSamples(float[] newSamples) {
            ConsumeSamples(newSamples, 0, newSamples.Length);
        }

        public void ConsumeSamples(float[] newSamples, int offset, int count) {
            for (int i = 0; i < count; i++) {
                var sample = newSamples[i + offset];

                if (sample < _min) _min = sample;
                if (sample > _max) _max = sample;

                ++_lastSampleCount;

                if (_lastSampleCount == Factor) {
                    AddMinMax(new MinMax { Max = _max, Min = _min });
                    _lastSampleCount = 0;
                    _min = float.MaxValue;
                    _max = float.MinValue;
                }
            }
        }
        
        public void AddMinMax(MinMax value) {
            Samples.Enqueue(value);
            ((MinMaxCache)NextMinMaxCache)?.ConsumeSample(value);
        }

        public void Clear() {
            Samples.Clear();
            _lastSampleCount = 0;
            _min = float.MaxValue;
            _max = float.MinValue;
        }
    }


    public class MinMaxCache : IMinMaxCache {

        private float _min;
        private float _max;
        private int   _lastSampleCount;

        public GenericRingBuffer<MinMax> Samples { get; }

        public int Factor { get; private set; }

        public int Ratio { get; private set; }

        public IMinMaxCache NextMinMaxCache { get; set; }


        public MinMaxCache(int factor, int previousFactor, int size) {
            Samples = new GenericRingBuffer<MinMax>(size) { FixedSize = true };
            _max = float.MinValue;
            _min = float.MaxValue;
            Factor = factor;
            Ratio = factor / previousFactor;
        }

        public void Clear() {
            _lastSampleCount = 0;
            Samples.Clear();
            _max = float.MinValue;
            _min = float.MaxValue;
        }

        public void ConsumeSample(MinMax sample) {
            if (sample.Max > _max) _max = sample.Max;
            if (sample.Min < _min) _min = sample.Min;

            ++_lastSampleCount;

            if (_lastSampleCount == Ratio) {
                var extrema = new MinMax {Max = _max, Min = _min };
                Samples.Enqueue(extrema);
                ((MinMaxCache)NextMinMaxCache)?.ConsumeSample(extrema);
                _lastSampleCount = 0;
                _max = float.MinValue;
                _min = float.MaxValue;
            }
        }


        /// <summary>
        /// creates a cascade of MinMax filters
        /// </summary>
        /// <param name="_zoomLevels">dividers by which to divide the initial samples by</param>
        /// <param name="initialSize">number of samples before dividing</param>
        /// <returns></returns>
        public static IMinMaxCache[] CreateCascade(int[] _zoomLevels, long initialSize) {
            var cascade = new IMinMaxCache[_zoomLevels.Length];
            cascade = new IMinMaxCache[_zoomLevels.Length];
            cascade[0] = new MinMaxCacheFirstStage(_zoomLevels[0], (int)(initialSize / _zoomLevels[0]));
            for (int i = 1; i < _zoomLevels.Length; i++) {
                cascade[i] = new MinMaxCache(_zoomLevels[i], _zoomLevels[i - 1], (int)(initialSize / _zoomLevels[i]));
                cascade[i - 1].NextMinMaxCache = cascade[i];
            }
            return cascade;
        }

        /// <summary>
        /// Save first level of MinMax cascade to an array
        /// </summary>
        /// <param name="fst"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException">Peek didn't read all samples</exception>
        public static MinMax[] FirstLevelToArray(MinMaxCacheFirstStage fst) {
            var result = new MinMax[fst.Samples.Length];
            var read = fst.Samples.Peek(result, 0, 0, result.Length);
            if (read != result.Length) {
                throw new InvalidOperationException("Peek didn't read expected number of samples, but should have");
            }
            return result;
        }

        public static IMinMaxCache[] CascadeFromFirstLevelArray(MinMax[] values, int[] _zoomLevels) {
            var cascade = CreateCascade(_zoomLevels, values.Length * _zoomLevels[0]);
            var fst     = (MinMaxCacheFirstStage) cascade[0];

            for (int i = 0; i < values.Length; i ++) {
                fst.AddMinMax(values[i]);
            }

            return cascade;
        }

        public static IMinMaxCache[] CascadeFromFile(string file, int[] _zoomLevels) {
            var f      = File.Open(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            var reader = new BinaryReader(f);

            var cascade = CreateCascade(_zoomLevels, f.Length / sizeof(float) / 2 * _zoomLevels[0]);
            var fst     = (MinMaxCacheFirstStage) cascade[0];
            var bound   = f.Length / sizeof(float) / 2;

            for (int i = 0; i < bound; i++) {
                var min = reader.ReadSingle();
                var max = reader.ReadSingle();
                fst.AddMinMax(new MinMax { Min = min, Max = max });
            }

            f.Close();

            return cascade;
        }

    }

}
