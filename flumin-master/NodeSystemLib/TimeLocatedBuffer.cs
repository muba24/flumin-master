using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NodeSystemLib {

    public class TimeLocatedBuffer : ICloneable {

        protected double[] _buffer;

        protected int _writtenSamples;

        public long Samplerate { get; set; }

        public TimeStamp CurrentTime { get; protected set; }

        public TimeStamp FrontTime => CurrentTime.Sub(WrittenSamples, Samplerate);

        public TimeStamp StampForSample(int index) {
            return FrontTime.Add(index, Samplerate);
        }

        public TimeLocatedBuffer(TimeLocatedBuffer cp) {
            _buffer         = new double[cp._buffer.Length];
            _writtenSamples = cp._writtenSamples;
            CurrentTime     = cp.CurrentTime;
            Samplerate      = cp.Samplerate;

            Array.Copy(
                destinationArray:   _buffer,
                sourceArray:        cp._buffer,
                length:             cp._buffer.Length
            );
        }

        public TimeLocatedBuffer(double[] buffer, int rate) : this(buffer, rate, TimeStamp.Zero()) {
        }

        public TimeLocatedBuffer(double[] buffer, int rate, TimeStamp lastStamp) {
            Samplerate  = rate;
            _buffer     = buffer;
            CurrentTime = new TimeStamp(lastStamp);
        }

        public TimeLocatedBuffer(int samples, int rate) {
            if (samples <= 0) throw new ArgumentOutOfRangeException(nameof(samples), "Samples must be > 0");
            Samplerate  = rate;
            _buffer     = new double[samples];
            CurrentTime = TimeStamp.Zero();
        }

        public void LoadState(object state) {
            if (!(state is TimeLocatedBuffer)) {
                throw new InvalidCastException($"Type of state object is not {nameof(TimeLocatedBuffer)}");
            }

            var buf         = (TimeLocatedBuffer)state;
            Samplerate      = buf.Samplerate;
            CurrentTime     = buf.CurrentTime;
            _writtenSamples = buf.WrittenSamples;
            
            if (_buffer.Length < buf._buffer.Length) {
                _buffer = new double[buf._buffer.Length];
            }

            Array.Copy(
                destinationArray: _buffer,
                sourceArray:      buf._buffer,
                length:           buf._buffer.Length
            );
        }

        public void ResetTime() {
            CurrentTime = TimeStamp.Zero();
        }

        public void SetTime(TimeStamp stmp) {
            CurrentTime = stmp;
        }

        /// <summary>
        /// Capacity of buffer
        /// </summary>
        public int Length => _buffer.Length;

        /// <summary>
        /// Number of valid samples in buffer
        /// </summary>
        public int WrittenSamples => _writtenSamples;

        public struct TimeLocatedBufferValueWithScalar {
            public double Value;
            public double Scalar;
            public TimeStamp Stamp;
        }

        public IEnumerable<TimeLocatedBufferValueWithScalar> ZipWithValueInput(ValueInputPort p) {
            var currentSampleTime = new TimeLocatedValue(0, new TimeStamp(0));

            for (int i = 0; i < WrittenSamples; i++) {
                var stamp = StampForSample(i);
                currentSampleTime.SetStamp(stamp);

                TimeLocatedValue value;
                if (p.Values.SafeTryWeakPredecessor(currentSampleTime, out value)) {
                    yield return new TimeLocatedBufferValueWithScalar { Scalar = value.Value, Value = _buffer[i], Stamp = stamp };
                    p.Values.SafeRemoveRangeTo(value);
                } else {
                    yield return new TimeLocatedBufferValueWithScalar { Scalar = 0, Value = _buffer[i], Stamp = TimeStamp.Zero() };
                }
            }

        }

        /// <summary>
        /// Sets how many samples got written to the buffer.
        /// Will reset the read pointer and increment buffer time.
        /// </summary>
        /// <param name="samples"></param>
        public void SetWritten(int samples) {
            _writtenSamples = samples;
            CurrentTime = CurrentTime.Add(samples, Samplerate);
        }

        public double[] GetSamples() => _buffer;

        public object Clone() {
            return new TimeLocatedBuffer(this);
        }

        public static TimeLocatedBuffer Default(int samplerate) {
            return new TimeLocatedBuffer(NodeSystemSettings.Instance.SystemHost.GetDefaultBufferSize(samplerate), samplerate);
        }
    }

}
