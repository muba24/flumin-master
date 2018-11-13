using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleADC {

    public class TimeLocatedBuffer : ICloneable {

        private readonly double[] _buffer;

        private int _writtenSamples;

        public long Samplerate { get; set; }

        public TimeStamp CurrentTime { get; private set; }

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

        public void ResetTime() {
            CurrentTime = TimeStamp.Zero();
        }

        public void SetTime(TimeStamp stmp) {
            CurrentTime = stmp;
        }

        public int Length => _buffer.Length;

        public int WrittenSamples => _writtenSamples;

        public void SetWritten(int samples) {
            _writtenSamples = samples;
            CurrentTime = CurrentTime.Add(samples, Samplerate);
        }

        public double[] GetSamples() => _buffer;

        public object Clone() {
            return new TimeLocatedBuffer(this);
        }

        public static TimeLocatedBuffer Default(int samplerate) {
            return new TimeLocatedBuffer(GlobalSettings.Instance.BufferSize(samplerate), samplerate);
        }
    }

}
