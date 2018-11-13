using System;
using System.Collections.Generic;
using System.Linq;

namespace WaveDisplayControl {

    public class LargeArray {

        private readonly List<double[]> _arrays = new List<double[]>();
        private long _skip;

        public void Skip(long samples) {
            _skip += samples;
            lock (_arrays) {
                while (_arrays.Count > 0 && _skip > _arrays[0].Length) {
                    _skip -= _arrays[0].Length;
                    _arrays.RemoveAt(0);
                }
            }
        }

        public void Clear() {
            lock (_arrays) {
                _arrays.Clear();
            }
        }

        public void Add(IEnumerable<double> arr) {
            lock (_arrays) {
                _arrays.Add(arr.ToArray());
            }
        }

        public void Add(double[] arr) {
            lock (_arrays) {
                _arrays.Add((double[])arr.Clone());
            }
        }

        public double GetValue(long index) {
            long sum = 0;

            lock (_arrays) {
                foreach (var arr in _arrays) {
                    var actualIndex = index + _skip - sum;
                    if (sum <= index && sum + arr.Length > actualIndex) {
                        return arr[actualIndex];
                    }
                    sum += arr.Length;
                }
            }

            throw new IndexOutOfRangeException();
        }

        public long Length {
            get {
                lock (_arrays) {
                    return _arrays.Sum(x => x?.LongLength ?? 0) - _skip;
                }
            }
        }

        public double this[long key] => GetValue(key);

        public long FillBuffer(long start, long count, long step, double[] buffer) {
            long sum = 0;
            var bufidx = 0;
            var arrIdx = -1;

            start += _skip;

            lock (_arrays) {
                for (var i = 0; i < _arrays.Count; i++) {
                    var actualIndex = start - sum;
                    if (sum <= start && sum + _arrays[i].Length > actualIndex) {
                        arrIdx = i;
                        break;
                    }
                    sum += _arrays[i].Length;
                }

                if (arrIdx == -1) return count;

                long j;

                var currentArray = _arrays[arrIdx];

                for (j = (int)(start - sum); count > 0 && j < currentArray.Length; j += step) {
                    buffer[bufidx++] = currentArray[j];
                    --count;
                }

                j -= currentArray.Length;
                arrIdx++;

                for (; count > 0 && arrIdx < _arrays.Count; arrIdx++) {
                    currentArray = _arrays[arrIdx];
                    var len = currentArray.Length;

                    for (; count > 0 && j < len; j += step) {
                        buffer[bufidx++] = currentArray[j];
                        --count;
                    }

                    j -= len;
                }
            }

            return count;

        }

        public IEnumerable<double> Iterate(long start, long count, long step) {
            long sum = 0;
            var arrIdx = -1;

            start += _skip;

            lock (_arrays) {

                for (var i = 0; i < _arrays.Count; i++) {
                    var actualIndex = start - sum;
                    if (sum <= start && sum + _arrays[i].Length > actualIndex) {
                        arrIdx = i;
                        break;
                    }
                    sum += _arrays[i].Length;
                }

                if (arrIdx == -1) yield break;

                long j;

                var currentArray = _arrays[arrIdx];

                for (j = (int)(start - sum); count > 0 && j < currentArray.Length; j += step) {
                    yield return currentArray[j];
                    --count;
                }

                j -= currentArray.Length;
                arrIdx++;

                for (; count > 0 && arrIdx < _arrays.Count; arrIdx++) {
                    currentArray = _arrays[arrIdx];
                    var len = currentArray.Length;

                    for (; count > 0 && j < len; j += step) {
                        yield return currentArray[j];
                        --count;
                    }

                    j -= len;
                }

            }
        }

    }

    public interface IWaveData {
        int Samplerate { get; }
        long Length { get; }
        IEnumerable<double> Iterate(long at, long count, long step);
        long GetData(long index, long count, long step, double[] buffer);
        void AddData(double[] data);
    }

    public class WaveData : IWaveData {

        public WaveData(int samplerate) {
            Samplerate = samplerate;
            MaxDurationMs = 0;
        }

        public int Samplerate { get; }

        public long Length => _arr.Length;

        private readonly LargeArray _arr = new LargeArray();

        public long MaxDurationMs {
            get; set;
        }

        public void AddData(double[] data) {
            _arr.Add(data);

            if (MaxDurationMs <= 0) return;
            var samples = (MaxDurationMs * Samplerate) / 1000;
            if (_arr.Length > samples) {
                _arr.Skip(_arr.Length - samples);
            }
        }

        public IEnumerable<double> Iterate(long at, long count, long step) {
            return _arr.Iterate(at, count, step);
        }

        public long GetData(long index, long count, long step, double[] buffer) {
            return _arr.FillBuffer(index, count, step, buffer);
        }
    }

}
