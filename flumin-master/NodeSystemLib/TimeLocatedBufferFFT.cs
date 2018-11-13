using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NodeSystemLib {
    public class TimeLocatedBufferFFT : TimeLocatedBuffer {

        public int FFTSize { get; }
        public int FrameSize => FFTSize / 2;
        public int FrameCapacity => Length / FrameSize;
        public int FramesAvailable => WrittenSamples / FrameSize;

        public TimeLocatedBufferFFT(int fftSize, int ffts, int samplerate) 
            : base(ffts * fftSize / 2, samplerate)
        {
            if (fftSize <= 0) throw new ArgumentException(nameof(fftSize));
            FFTSize = fftSize;
        }

        public void SetFramesWritten(int count) {
            _writtenSamples = count * FrameSize;
            CurrentTime = CurrentTime.Add(count * FFTSize, Samplerate);
        }

    }
}
