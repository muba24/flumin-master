using NodeSystemLib2.FormatValue;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NodeSystemLib2.FormatData1D {

    public class TimeLocatedBuffer1D<T> : TimeLocatedBuffer, IReadOnlyTimeLocatedBuffer1D<T> where T : struct {

        #region SampleTimeValueCombinations

        public struct SampleTimeValueCombination {
            public T Sample { get; }
            public T Scalar { get; }
            public TimeStamp Stamp { get; }

            public SampleTimeValueCombination(T sample, T scalar, TimeStamp stamp) {
                Sample = sample;
                Scalar = scalar;
                Stamp = stamp;
            }
        }

        public struct SampleTimeValueCombination2 {
            public T Sample { get; }
            public T Scalar { get; }
            public T Scalar2 { get; }
            public TimeStamp Stamp { get; }

            public SampleTimeValueCombination2(T sample, T scalar, T scalar2, TimeStamp stamp) {
                Sample = sample;
                Scalar = scalar;
                Scalar2 = scalar2;
                Stamp = stamp;
            }
        }

        #endregion

        public TimeLocatedBuffer1D(int elements, int samplerate) {
            Samplerate = samplerate;
            Data       = new T[elements];
        }

        public T[] Data { get; }

        public int Samplerate { get; }

        public int Capacity => Data.Length;

        public int Available { get; private set; }

        public void SetWritten(int elements) {
            Available = elements;
            Time      = Time.Increment(elements, Samplerate);
        }

        public void SetWritten(int elements, TimeStamp stamp) {
            Available = elements;
            Time = stamp;
        }

        public TimeStamp StampForSample(int index) {
            return Time.Decrement(Available - index, Samplerate);
        }

        public IEnumerable<SampleTimeValueCombination> ZipWithValueInput(InputPortValue<T> p) {
            var it = p.GetIterator(StampForSample(0));

            for (int i = 0; i < Available; i++) {
                var stamp = StampForSample(i);
                it.Advance(stamp);
                yield return new SampleTimeValueCombination(Data[i], it.CurrentItem.Value, stamp);
            }

        }

        public IEnumerable<SampleTimeValueCombination2> ZipWithValueInput(InputPortValue<T> p, InputPortValue<T> p2) {
            var it = p.GetIterator(StampForSample(0));
            var it2 = p2.GetIterator(StampForSample(0));

            for (int i = 0; i < Available; i++) {
                var stamp = StampForSample(i);
                it.Advance(stamp);
                it2.Advance(stamp);
                yield return new SampleTimeValueCombination2(Data[i], it.CurrentItem.Value, it2.CurrentItem.Value, stamp);
            }

        }

    }

    public interface IReadOnlyTimeLocatedBuffer1D<T> where T : struct {

        T[] Data { get; }

        int Samplerate { get; }

        int Capacity { get; }

        int Available { get; }

        TimeStamp StampForSample(int index);

        TimeStamp Time { get; }

        IEnumerable<TimeLocatedBuffer1D<T>.SampleTimeValueCombination> ZipWithValueInput(InputPortValue<T> p);

        IEnumerable<TimeLocatedBuffer1D<T>.SampleTimeValueCombination2> ZipWithValueInput(InputPortValue<T> p, InputPortValue<T> p2);

    }

}
